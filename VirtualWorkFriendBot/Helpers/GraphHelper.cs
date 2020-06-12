using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace VirtualWorkFriendBot.Helpers
{
    public class GraphHelper
    {
        private static IConfiguration Configuration { get; set; }
        public static void Configure(IConfiguration config)
        {
            Configuration = config;
        }
        public static string GraphAppId { 
            get
            {
                return Configuration?["GraphAppId"];
            }
        }

        private const string SECTION_PAGES_TEMPLATE = "https://graph.microsoft.com/v1.0/me/onenote/sections/{0}/pages";
        private readonly string _token;
        private GraphServiceClient _authenticatedClient;
        private HttpClient _authenticatedHttpClient;

        public GraphHelper(string token) : this(token, false) { }
        public GraphHelper(string token, bool authenticate)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            _token = token;            
            if (authenticate)
            {
                Authenticate();
            }
        }

        #region General
        // Sends an email on the users behalf using the Microsoft Graph API
        public async Task SendMailAsync(string toAddress, string subject, string content)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                throw new ArgumentNullException(nameof(toAddress));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentNullException(nameof(content));
            }

            var graphClient = GetAuthenticatedClient();
            var recipients = new List<Recipient>
        {
            new Recipient
            {
                EmailAddress = new EmailAddress
                {
                    Address = toAddress,
                },
            },
        };

            // Create the message.
            var email = new Message
            {
                Body = new ItemBody
                {
                    Content = content,
                    ContentType = BodyType.Text,
                },
                Subject = subject,
                ToRecipients = recipients,
            };

            // Send the message.
            await graphClient.Me.SendMail(email, true).Request().PostAsync();
        }

        // Gets mail for the user using the Microsoft Graph API
        public async Task<Message[]> GetRecentMailAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var messages = await graphClient.Me.MailFolders.Inbox.Messages.Request().GetAsync();
            return messages.Take(5).ToArray();
        }

        // Get information about the user.
        public async Task<User> GetMeAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var me = await graphClient.Me.Request().GetAsync();
            return me;
        }

        // gets information about the user's manager.
        public async Task<User> GetManagerAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var manager = await graphClient.Me.Manager.Request().GetAsync() as User;
            return manager;
        }




        #region PhotoResponse
        /*
        // Gets the user's photo
        public async Task<PhotoResponse> GetPhotoAsync()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            using (var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value"))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Graph returned an invalid success code: {response.StatusCode}");
                }

                var stream = await response.Content.ReadAsStreamAsync();
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);

                var photoResponse = new PhotoResponse
                {
                    Bytes = bytes,
                    ContentType = response.Content.Headers.ContentType?.ToString(),
                };

                if (photoResponse != null)
                {
                    photoResponse.Base64String = $"data:{photoResponse.ContentType};base64," +
                                                    Convert.ToBase64String(photoResponse.Bytes);
                }

                return photoResponse;
            }
        }
        */
        #endregion
        #endregion

        // Get an Authenticated Microsoft Graph client using the token issued to the user.
        public GraphServiceClient GetAuthenticatedClient()
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    requestMessage =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", _token);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Local.Id + "\"");

                        return Task.CompletedTask;
                    }));
            return graphClient;
        }
        public HttpClient GetAuthenticatedHttpClient()
        {
            var authValue = new AuthenticationHeaderValue("Bearer", _token);

            var client = new HttpClient()
            {
                DefaultRequestHeaders = { Authorization = authValue }
                //Set some other client defaults like timeout / BaseAddress
            };
            return client;
        }
        public void Authenticate()
        {
            if (_authenticatedClient == null)
            {
                _authenticatedClient = GetAuthenticatedClient();
            }
            if (_authenticatedHttpClient == null)
            {
                _authenticatedHttpClient = GetAuthenticatedHttpClient();
            }
        }
        public GraphServiceClient AuthenticatedClient
        {
            get
            {
                return _authenticatedClient;
            }
        }
        public HttpClient AuthenticatedHttpClient
        {
            get
            {
                return _authenticatedHttpClient;
            }
        }

        private GraphServiceClient GetAuthenticatedClient(GraphServiceClient graphClient)
        {
            if (graphClient == null)
            {
                Authenticate();
                return _authenticatedClient;
            }
            else
            {
                return graphClient;
            }
        }
        private HttpClient GetAuthenticatedHttpClient(HttpClient httpClient)
        {
            if (httpClient == null)
            {
                Authenticate();
                return _authenticatedHttpClient;
            }
            else
            {
                return httpClient;
            }
        }

        public async Task<List<Notebook>> GetNotebooks(GraphServiceClient graphClient = null)
        {
            graphClient = GetAuthenticatedClient(graphClient);
            var notebooks = await graphClient.Me.Onenote.Notebooks
                .Request()
                .GetAsync();
            var allNotebooks = notebooks.ToList();
            allNotebooks.Insert(0, new Notebook
            {
                DisplayName = "New Notebook"
            });
            return allNotebooks;
        }
        public async Task<Notebook> GetNotebook(string notebookId, GraphServiceClient graphClient = null)
        {
            graphClient = GetAuthenticatedClient(graphClient);
            var notebook = await graphClient.Me.Onenote
                .Notebooks[notebookId]
                .Request()
                .GetAsync();            
            return notebook;
        }

        public async Task<List<OnenoteSection>> GetNotebookSections(string notebookId, GraphServiceClient graphClient = null )
        {
            graphClient = GetAuthenticatedClient(graphClient);
            var sections = await graphClient.Me.Onenote
                .Notebooks[notebookId]
                .Sections
                .Request()
                .GetAsync();
            return sections.ToList();
        }
        public async Task<Notebook> CreateNotebook(string name, GraphServiceClient graphClient = null)
        {
            graphClient = GetAuthenticatedClient(graphClient);

            var notebook = new Notebook
            {
                DisplayName = name
            };

            await graphClient.Me.Onenote.Notebooks
                .Request()
                .AddAsync(notebook);
            return notebook;
        }
        
        public async Task<OnenoteSection> CreateNotebookSection(string notebookId, string sectionName, GraphServiceClient graphClient = null)
        {
            graphClient = GetAuthenticatedClient(graphClient);

            var newSection = new OnenoteSection
            {
                DisplayName = sectionName
            };


            newSection = await graphClient.Me.Onenote.Notebooks[notebookId].Sections
                .Request()
                .AddAsync(newSection);
            return newSection;
        }

        public async Task<OnenotePage> CreateNotebookPage(
            string sectionId,
            string html,
            HttpClient httpClient = null)
        {
            httpClient = GetAuthenticatedHttpClient(httpClient);

            Uri Uri = new Uri($"https://graph.microsoft.com/v1.0/users/me/onenote/sections/{sectionId}/pages");

            HttpContent httpContent = new StringContent(html, System.Text.Encoding.UTF8, "application/xhtml+xml");

            var response = await httpClient.PostAsync(Uri, httpContent);

            var json = await response.Content.ReadAsStringAsync();
            OnenotePage page = null;
            try
            {
                page = JsonConvert.DeserializeObject<OnenotePage>(json);
            }
            catch (Exception)
            {

            }

            return page;
        }

        public async Task<bool> UpdateNotebookPageLevel(
            string pageId, int pageLevel,
            HttpClient httpClient = null)
        {
            httpClient = GetAuthenticatedHttpClient(httpClient);

            Uri Uri = new Uri($"https://graph.microsoft.com/v1.0/me/onenote/pages/{pageId}");

            var body = JsonConvert.SerializeObject(new { level = pageLevel });

            HttpContent httpContent = new StringContent(body
                , System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PatchAsync(Uri, httpContent);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                response = await httpClient.PatchAsync(Uri, httpContent);
            }
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> UpdateNotebookPageOrder(
            string pageId, int pageOrder,
            HttpClient httpClient = null)
        {
            httpClient = GetAuthenticatedHttpClient(httpClient);

            Uri Uri = new Uri($"https://graph.microsoft.com/v1.0/me/onenote/pages/{pageId}");

            var body = JsonConvert.SerializeObject(new { order = pageOrder });

            HttpContent httpContent = new StringContent(body
                , System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PatchAsync(Uri, httpContent);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<OnenotePage>> GetNotebookPagesInSection(OnenoteSection section, HttpClient httpClient = null)
        {
            string properties = "id,level,order,title,createdDateTime,createdByAppId,lastModifiedDateTime";
            string pageSort = "order";//"createdDateTime";
            string baseSectionPagesUrl = string.Format(SECTION_PAGES_TEMPLATE, section.Id);

            httpClient = GetAuthenticatedHttpClient(httpClient);

            UriBuilder uriBuilder = new UriBuilder(section.PagesUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["top"] = "37"; // 6 header pages + up to 31 daily journal pages
            query["pagelevel"] = "true"; // adds level and ordinal properties
            query["$select"] = properties;
            query["$orderby"] = pageSort;
            query["filter"] = $"createdByAppId eq '{GraphHelper.GraphAppId}'";
            uriBuilder.Query = query.ToString();
            var pUri = uriBuilder.Uri;
            var response = await httpClient.GetAsync(pUri);

            var json = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);
            var rawValues = root.GetValue("value");
            List<OnenotePage> pages = new List<OnenotePage>();
            try
            {
                pages = JsonConvert.DeserializeObject<List<OnenotePage>>(rawValues.ToString());

            }
            catch (Exception)
            {

            }

            return pages.OrderBy(p => p.Order).ToList();
        }
        public async Task<string> GetNotebookPageContent(string pageId,
            HttpClient httpClient = null)
        {
            string body = String.Empty;
            httpClient = GetAuthenticatedHttpClient(httpClient);

            Uri Uri = new Uri($"https://graph.microsoft.com/v1.0/me/onenote/pages/{pageId}/content");

            var response = await httpClient.GetAsync(Uri);
            if (response.IsSuccessStatusCode)
            {
                body = await response.Content.ReadAsStringAsync();
            }
            return body;
        }

        public async Task<OnenotePage> GetNotebookPage(string pageId, GraphServiceClient graphClient = null)
        {
            graphClient = GetAuthenticatedClient(graphClient);
            var page = await graphClient.Me.Onenote
                .Pages[pageId]
                .Request()
                .GetAsync();
            return page;
        }
    }
}
