using Azure.AI.TextAnalytics;
using HtmlAgilityPack;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BS = Microsoft.Bot.Schema;
using VirtualWorkFriendBot.Helpers;
using VirtualWorkFriendBot.Models;

namespace VirtualWorkFriendBot.Dialogs
{
    public class JournalingDialog : VirtualFriendDialog
    {
        private GraphHelper _graphHelper;

        public JournalingDialog (
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(JournalingDialog), serviceProvider, telemetryClient)
        {
            InitialDialogId = nameof(OnboardingDialog);

            var onboarding = new WaterfallStep[]
            {
                    SetupDialog,

                    SelectNotebookPrompt,
                    SelectNotebookProcess,

                    GetCurrentSection,
                    CheckTodaysEntry,

                    CompleteDialog
            };

            AddDialog(new WaterfallDialog(InitialDialogId, onboarding) { TelemetryClient = telemetryClient });

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
        }


        protected async override Task PopulateStateObjects(WaterfallStepContext sc)
        {
            await base.PopulateStateObjects(sc);
            _graphHelper = new GraphHelper(_discussionState.UserToken.Token);
        }
        #region Waterfall Steps
        private async Task<DialogTurnResult> SetupDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> SelectNotebookPrompt(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                "Retrieving your list of OneNote notebooks"));
            
            var notebooks = await _graphHelper.GetNotebooks();

            AddUpdateStepContextValue(stepContext, "Notebooks", notebooks);

            var notebookId = _onboardingState.Journal.NotebookId;

            if (!String.IsNullOrEmpty(notebookId))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                    "Validating your journal is still available."));

                // validate the notebook Id is valid
                var notebook = notebooks.FirstOrDefault(n => n.Id == notebookId);
                if (notebook != null)
                {
                    // notebook is still valid so skip to the next step
                    return await stepContext.NextAsync(
                        new FoundChoice { 
                            Value = notebook.DisplayName 
                        }, cancellationToken);
                }
            }

            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text("Which OneNote notebook would you like to use for journaling?"),
                Choices = notebooks.Select(n => new Choice(n.DisplayName)).ToList(),
                Style = ListStyle.List
            };

            return await stepContext.PromptAsync(nameof(ChoicePrompt), options, cancellationToken);
        }
        private async Task<DialogTurnResult> SelectNotebookProcess(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            var notebooks = (List<Notebook>)stepContext.Values["Notebooks"];
            var sectionId = String.Empty;
            AddUpdateStepContextValue(stepContext, "NotebookCreated", false);

            Notebook currentNotebook = new Notebook();
            FoundChoice selected = stepContext.Result as FoundChoice;
            if (selected != null)
            {
                currentNotebook = notebooks.FirstOrDefault(n => n.DisplayName == selected.Value);
            }
            if (String.IsNullOrEmpty(currentNotebook.Id))
            {
                // Try to find the notebook with the default name

                string notebookName = JournalHelper.DefaultNotebookName;
                currentNotebook = notebooks.FirstOrDefault(n => n.DisplayName == notebookName);
                if (currentNotebook == null)
                {
                    // Get or create the notebook
                    _graphHelper = new GraphHelper(_discussionState.UserToken.Token, true);

                    currentNotebook = await _graphHelper.CreateNotebook(notebookName);
                    AddUpdateStepContextValue(stepContext, "NotebookCreated", true);
                }
            }
            _onboardingState.Journal.NotebookId = currentNotebook.Id;
            await StateHelper.PersistInStateAsync<UserState, OnboardingState>(
                _serviceProvider, stepContext.Context, _onboardingState, true);
            AddUpdateStepContextValue(stepContext, "Notebook", currentNotebook);
            return await stepContext.NextAsync(sectionId, cancellationToken);
        }

        private async Task<DialogTurnResult> GetCurrentSection(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);
            _graphHelper.Authenticate();

            var today = DateTime.Today;
            
            var notebookId = _onboardingState.Journal.NotebookId;
            var section = await GetOrCreateNotebookSection(notebookId, today);

            AddUpdateStepContextValue(stepContext, "Section", section);

            var sectionId = section.Id;
            return await stepContext.NextAsync(sectionId, cancellationToken);
        }

        private async Task<DialogTurnResult> CheckTodaysEntry(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            _graphHelper.Authenticate();

            var section = stepContext.Values["Section"] as OnenoteSection;
            var contextId = _discussionState.SignedInUserId;
            var currentPages = await _graphHelper.GetNotebookPagesInSection(section);

            DateTime entryDate = DateTime.Today;

            List<JournalPageMetadata> journalPages = new List<JournalPageMetadata>();
            int currentDay = entryDate.Day;
            int daysInMonth = DateTime.DaysInMonth(entryDate.Year, entryDate.Month);
            int year = entryDate.Year;
            int month = entryDate.Month;
            List<JournalEntry> userEntries = DBHelper.GetJournalEntries(contextId,
                new DateTime(year, month, 1),
                new DateTime(year, month, daysInMonth));

            var invalidEntries = new KeyValuePair<string, List<string>>(
                contextId, new List<string>());

            // Remove pages that do not exist in the current pages collection
            for (int i = userEntries.Count - 1; i >= 0; i--)
            {
                var expectedPage = userEntries[i];
                if (!currentPages.Exists(ce => ce.Id == expectedPage.Id))
                {
                    invalidEntries.Value.Add(expectedPage.Id);
                    userEntries.Remove(expectedPage);
                }
            }
            if (invalidEntries.Value.Count > 0)
            {
                DBHelper.RemoveInvalidJournalEntries(invalidEntries);
            }

            var sentimentNeeded = userEntries.Where(j => {
                var cp = currentPages.First(p => p.Id == j.Id);
                return (
                    (j.Sentiment == null) || 
                    ((j.Sentiment != null) && 
                        (cp.LastModifiedDateTime > j.LastModified)));
            }).ToList();
            if (sentimentNeeded.Count > 0)
            {
                var sentimentDocuments = new Dictionary<String, TextDocumentInput[]>();
                foreach (var item in sentimentNeeded)
                {
                    var prompt = "Checking out your" +
                        (item.Sentiment != null ? " updated" : "") +
                        $" entry for {item.EntryDate.ToLongDateString()}";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(prompt));
                    string content = await _graphHelper.GetNotebookPageContent(item.Id);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(content);
                    var body = doc.DocumentNode.SelectSingleNode("//body");
                    var divHeader = doc.DocumentNode.SelectSingleNode("//div[@data-id='_default']");

                    var paragraphs = new List<String>();
                    var paragraphNodes = divHeader.SelectNodes(".//p");
                    if (paragraphNodes?.Count > 0)
                    {
                        paragraphs.AddRange(paragraphNodes.Select(p => p.InnerText).ToArray());
                    }

                    var remainingHtml = body.InnerHtml.Replace(divHeader.OuterHtml, "");
                    var remainingDoc = new HtmlDocument();
                    remainingDoc.LoadHtml(remainingHtml);

                    paragraphNodes = remainingDoc.DocumentNode.SelectNodes("//p");
                    if (paragraphNodes?.Count > 0)
                    {
                        paragraphs.AddRange(paragraphNodes.Select(p => p.InnerText).ToArray());
                    }

                    if (paragraphs.Count > 0)
                    {
                        var combinedText = System.Web.HttpUtility.HtmlDecode(String.Join("\n", paragraphs));
                        AddInputDocument(sentimentDocuments, item.Id, combinedText);
                        prompt = "That's new.. I'll have to consider what you wrote.";
                    }
                    else
                    {
                        prompt = "Nothing new for me to check out here!";
                    }
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(prompt));
                }

                if (sentimentDocuments.Count > 0)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                        "Assessing what you wrote..."));
                }

                var sentimentResults = TextAnalyticsHelper.GetSentiment(sentimentDocuments);
                foreach (var entryKey in sentimentResults.Keys)
                {
                    var assessedPage = currentPages.First(e => e.Id == entryKey);
                    var lastModified = assessedPage.LastModifiedDateTime;
                    var ds = sentimentResults[entryKey];
                    DBHelper.SaveJournalEntryAssessment(new JournalEntryAssessment
                    {
                        Id = entryKey,
                        Sentiment = ds,
                        LastModified = lastModified?.UtcDateTime
                    });

                    var prompt = $"Your entry on {assessedPage.Title} was {ds.Sentiment}";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(prompt));
                }
            }

            var monthMode = false;
            var createdPage = false;
            OnenotePage currentPage = null;
            var pageId = String.Empty;

            //Month mode
            if (monthMode)
            {
                for (int i = currentDay; i <= daysInMonth; i++)
                {
                    DateTime current = new DateTime(year, month, i);
                    if (!userEntries.Exists(e => e.EntryDate == current))
                    {
                        journalPages.Add(new JournalPageMetadata(current));
                    }
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                    "Looking for today's journal entry"));
                var existingPage = userEntries.FirstOrDefault(e => e.EntryDate == entryDate);
                if (existingPage == null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                        "Hmmm... didn't find today's entry. Don't worry I'll create one for you."));
                    journalPages.Add(new JournalPageMetadata(entryDate));
                }
                else
                {
                    pageId = existingPage.Id;
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                        "Found it!"));
                }
            }

            if (String.IsNullOrEmpty(pageId))
            {
                var weeks = journalPages.GroupBy(p => p.Week).ToList();
                var pageTemplate = ResourceHelper
                    .ReadManifestData<JournalingDialog>("JournalPageTemplate.htm");
                var headerPageTemplate = ResourceHelper
                    .ReadManifestData<JournalingDialog>("JournalHeaderTemplate.htm");

                foreach (var grp in weeks)
                {
                    var headerPageTitle = $"Week {grp.Key}";
                    var headerEntryDate = entryDate.ToString("yyyy-MM-ddTHH:mm:ss.0000000");
                    Console.Write($" {headerPageTitle} ");
                    var headerPage = currentPages.FirstOrDefault(p => p.Title == headerPageTitle);

                    if (headerPage == null)
                    {
                        var headerHtml = String.Format(headerPageTemplate, headerPageTitle, headerEntryDate);
                        var headerPageId = await _graphHelper.CreateNotebookPage(section.Id, headerHtml);
                        Console.WriteLine(headerPageId);
                    }
                    else
                    {
                        Console.WriteLine(headerPage.Id);
                    }

                    foreach (var item in grp)
                    {
                        var jp = DBHelper.GetJournalPrompt();

                        var pageTitle = $"{item.DayOfWeek} the {item.Day.AsOrdinal()}";
                        Console.Write($"\t{pageTitle} ");
                        var newEntryDate = item.EntryDate.ToString("yyyy-MM-ddTHH:mm:ss.0000000");

                        var prompt = jp.Prompt;
                        var details = jp.Details;
                        var promptSource = jp.Source;

                        var html = String.Format(pageTemplate, pageTitle, newEntryDate,
                            prompt, details, promptSource,
                            entryDate.Ticks, jp.SourceIndex, jp.PromptIndex);

                        currentPage = await _graphHelper.CreateNotebookPage(section.Id, html);
                        pageId = currentPage.Id;
                        createdPage = true;
                        var je = new JournalEntry
                        {
                            UserContextId = contextId,
                            Id = pageId,
                            PromptSourceId = jp.SourceIndex,
                            PromptIndexId = jp.PromptIndex,
                            EntryDate = item.EntryDate
                        };
                        DBHelper.CreateJournalEntry(je);
                        await _graphHelper.UpdateNotebookPageLevel(pageId, 1);

                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                            "All done setting up today's entry for you!. Ping me when you want me to take a look."));
                        Console.WriteLine($"{pageId}");
                    }
                }
            }

            if ((currentPage == null) && !String.IsNullOrEmpty(pageId))
            {
                currentPage = await _graphHelper.GetNotebookPage(pageId);
            }
            AddUpdateStepContextValue(stepContext, "CurrentPage", currentPage);
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> CompleteDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {            
            var currentNotebook = stepContext.Values["Notebook"] as Notebook;
            var currentPage = stepContext.Values["CurrentPage"] as OnenotePage;

            Action<List<BS.CardAction>> fnAddButtons = new Action<List<BS.CardAction>>((buttons) =>
            {
                buttons.Add(CreateOpenUrlAction(
                    "Open my journal in OneNote",
                    currentNotebook.Links.OneNoteClientUrl.Href));
                buttons.Add(CreateOpenUrlAction(
                    "Open my journal on the web",
                    currentNotebook.Links.OneNoteWebUrl.Href));
                buttons.Add(CreateOpenUrlAction(
                    "Open today's entry in OneNote",
                    currentPage.Links.OneNoteClientUrl.Href));
                buttons.Add(CreateOpenUrlAction(
                    "Open today's entry on the web",
                    currentPage.Links.OneNoteWebUrl.Href));
            });

            await CreateLinkCard(stepContext, cancellationToken, "In case you need it:", fnAddButtons);

            return await EndDialogAndProcessing(stepContext, cancellationToken);
        }
        #endregion

        #region Supporting Members
        private static void AddUpdateStepContextValue<T>(WaterfallStepContext stepContext, string name, T value)
        {
            if (!stepContext.Values.TryAdd(name, value))
            {
                stepContext.Values[name] = value;
            }
        }
        private async Task<OnenoteSection> GetOrCreateNotebookSection(string notebookId, DateTime from)
        {
            _graphHelper.Authenticate();
            // Create the section
            var info = new DateTimeFormatInfo();
            string sectionName = $"{from.Year.ToString()}-{info.MonthNames[from.Month - 1]}";
            var sectionId = String.Empty;
            var sections = await _graphHelper.GetNotebookSections(notebookId);
            var section = sections.FirstOrDefault(s => s.DisplayName == sectionName);
            if (section == null)
            {
                section = await _graphHelper.CreateNotebookSection(notebookId, sectionName);
            }

            return section;
        }

        #endregion

        #region Sentiment Analysis
        public static void AddInputDocument(
            Dictionary<String, TextDocumentInput[]> documents,
            string documentKey, string data)
        {
            // Split the data into buffers of size
            var parts = data.SplitInParts(1000).ToArray();
            // Create the TextDocument Inputs
            var newValue = parts.Select((part, index) =>
            {
                var newId = $"{documentKey} {index}";
                return new TextDocumentInput(newId, part.ToString());
            }).ToArray();

            // add the TextDocumentArray
            documents.Add(documentKey, newValue);
        }

        #endregion
    }
}
