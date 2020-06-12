using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;


namespace VirtualWorkFriendBot.Helpers
{
    public class TextAnalyticsHelper
    {
        public static IConfiguration Configuration { get; set; }
        private static TextAnalyticsApiKeyCredential credentials;
        private static Uri endpoint;
        public static void Configure(IConfiguration configuration)
        {
            var config = configuration.GetSection("TextAnalytics");
            credentials = new TextAnalyticsApiKeyCredential(config["Key"]);
            endpoint = new Uri(config["Endpoint"]);
        }

        public static DocumentSentiment GetSentiment(string input)
        {
            var client = new TextAnalyticsClient(endpoint, credentials);
            DocumentSentiment documentSentiment = client.AnalyzeSentiment(input);
            Console.WriteLine($"Document sentiment: {documentSentiment.Sentiment}\n");

            var si = new StringInfo(input);
            foreach (var sentence in documentSentiment.Sentences)
            {
                Console.WriteLine($"\tSentence [length {sentence.GraphemeLength}]");
                Console.WriteLine($"\tText: \"{si.SubstringByTextElements(sentence.GraphemeOffset, sentence.GraphemeLength)}\"");
                Console.WriteLine($"\tSentence sentiment: {sentence.Sentiment}");
                Console.WriteLine($"\tPositive score: {sentence.ConfidenceScores.Positive:0.00}");
                Console.WriteLine($"\tNegative score: {sentence.ConfidenceScores.Negative:0.00}");
                Console.WriteLine($"\tNeutral score: {sentence.ConfidenceScores.Neutral:0.00}\n");
            }
            return documentSentiment;
        }
        public static Dictionary<string, DocumentSentiment> GetSentiment(Dictionary<string, TextDocumentInput[]> documents)
        {
            Dictionary<string, DocumentSentiment> results = new Dictionary<string, DocumentSentiment>();
            List<TextDocumentInput> inputs = new List<TextDocumentInput>();
            documents.Values.ToList().ForEach(v => inputs.AddRange(v));

            if (inputs.Count > 0) { 
                var client = new TextAnalyticsClient(endpoint, credentials);
                var response = client.AnalyzeSentimentBatch(inputs);
                var batchResults = response.Value;

                documents.Keys.ToList().ForEach(entryId =>
                {
                    var entryResults = batchResults.Where(br =>
                    {
                        var idParts = br.Id.Split(" ");
                        return (idParts[0] == entryId);
                    }).ToList();
                    results.Add(entryId, CombineSentiments(documents[entryId], entryResults));
                });
            }
            return results;
        }

        private class SentimentComponent
        {
            public DocumentSentiment Result { get; private set; }
            public double Length { get; private set; }
            public double Factor { get; set; }

            public SentimentComponent(DocumentSentiment result, double textLength)
            {
                this.Result = result;
                this.Part = JObject.FromObject(result);
                this.Length = textLength;
            }
            public JObject Part { get; set; }
        }

        public static DocumentSentiment ToDocumentSentiment(string input)
        {
            return ToDocumentSentiment(JObject.Parse(input));
        }
        public static DocumentSentiment ToDocumentSentiment(JObject input)
        {
            var ti = typeof(DocumentSentiment).GetTypeInfo().DeclaredConstructors.First();

            Type[] paramTypes = new Type[] {
                typeof(TextSentiment),
                typeof(double),
                typeof(double),
                typeof(double),
                typeof(List<SentenceSentiment>)
            };

            var score = ToSentimentConfidenceScores(input);

            var sentences = input.Value<JArray>("Sentences")
                        .Select(s => ToSentenceSentiment((JObject)s)).ToList();


            TextSentiment ts = TextSentiment.Neutral;
            #region GetDocumentOverall Sentiment
            int sentencesPositive = sentences.Count(s =>
                GetSentenceTextSentiment(s) == TextSentiment.Positive);
            int sentencesNegative = sentences.Count(s =>
                GetSentenceTextSentiment(s) == TextSentiment.Negative);
            int sentencesNeutral = sentences.Count(s =>
                GetSentenceTextSentiment(s) == TextSentiment.Neutral);
            if ((sentencesPositive > 0) && (sentencesNegative > 0))
            {
                ts = TextSentiment.Mixed;
            }
            else if (sentencesPositive > 0)
            {
                ts = TextSentiment.Positive;
            }
            else if (sentencesNegative > 0)
            {
                ts = TextSentiment.Negative;
            } 
            #endregion

            object[] paramValues = new object[] {
                ts,
                score.Positive,
                score.Neutral,
                score.Negative,
                sentences
            };

            DocumentSentiment instance = null;
            try
            {
                instance = TypeHelpers.Construct<DocumentSentiment>(
                    paramTypes, paramValues);
            }
            catch (Exception)
            {
            }

            return instance;
        }

        private static TextSentiment GetSentenceTextSentiment(SentenceSentiment input)
        {
            var score = input.ConfidenceScores;

            var maxScore = (new Double[] { score.Positive, score.Negative, score.Neutral }).Max();
            if (maxScore == score.Positive)
            {
                return TextSentiment.Positive;
            } else if ( maxScore == score.Negative )
            {
                return TextSentiment.Negative;
            }
            return TextSentiment.Neutral;
        }

        public static SentenceSentiment ToSentenceSentiment(string input)
        {
            return ToSentenceSentiment(JObject.Parse(input));
        }
        public static SentenceSentiment ToSentenceSentiment(JObject input)
        {
            var ti = typeof(SentenceSentiment).GetTypeInfo().DeclaredConstructors.First();

             
            Type[] paramTypes = new Type[] {
                typeof(TextSentiment),
                typeof(double),
                typeof(double),
                typeof(double),
                typeof(int),
                typeof(int)
            };

            TextSentiment ts = (TextSentiment)input.Value<int>("Sentiment");
            var score = ToSentimentConfidenceScores(input.Value<JObject>("ConfidenceScores"));

            object[] paramValues = new object[] {
                ts,
                score.Positive,
                score.Neutral,
                score.Negative,
                input.Value<int>("GraphemeOffset"),
                input.Value<int>("GraphemeLength")
            };

            SentenceSentiment instance = default(SentenceSentiment);
            try
            {
                instance = TypeHelpers.Construct<SentenceSentiment>(
                    paramTypes, paramValues);
            }
            catch (Exception)
            {
            }
            
            return instance;            
        }

        public static SentimentConfidenceScores ToSentimentConfidenceScores(string input)
        {
            return ToSentimentConfidenceScores(JObject.Parse(input));
        }
        public static SentimentConfidenceScores ToSentimentConfidenceScores(JObject input )
        {

            Type[] paramTypes = new Type[] { 
                typeof(double), 
                typeof(double), 
                typeof(double) };
            
            object[] paramValues = new object[] { 
                input.Value<double>("Positive"),
                input.Value<double>("Neutral"),
                input.Value<double>("Negative")};

            var instance =TypeHelpers.Construct<SentimentConfidenceScores>(
                paramTypes, paramValues);
            return instance;
        }

        private static DocumentSentiment CombineSentiments(TextDocumentInput[] textDocumentInput, List<AnalyzeSentimentResult> entryResults)
        {
            DocumentSentiment ds = null;
            if (textDocumentInput.Length == 1)
            {
                var id = textDocumentInput[0].Id;
                ds = entryResults.FirstOrDefault(r => r.Id == id).DocumentSentiment;
            }
            else
            {
                var data = String.Empty;
                var results = new List<SentimentComponent>();
                foreach (var item in textDocumentInput)
                {                    
                    var sampleId = item.Id;
                    var sample = new SentimentComponent(
                        entryResults.FirstOrDefault(r => r.Id == sampleId)
                            .DocumentSentiment,
                        item.Text.Length);
                    results.Add(sample);
                }

                // get the total length
                double textLength = results.Sum(r => r.Length);

                // create the factor
                results.ForEach(r => r.Factor = r.Length / textLength);

                // get the weighted scores
                double positive = Math.Round(results.Sum(r => r.Result.ConfidenceScores.Positive * r.Factor) ,2);
                double negative = Math.Round(results.Sum(r => r.Result.ConfidenceScores.Negative * r.Factor),2);
                double neutral = Math.Round(results.Sum(r => r.Result.ConfidenceScores.Neutral * r.Factor),2);

                var cs = results[0].Part.Value<JObject>("ConfidenceScores");
                cs["Positive"] = positive;
                cs["Negative"] = negative;
                cs["Neutral"] = neutral;
                var summarySentences = results[0].Part.Value<JArray>("Sentences");
                var offset = 0;
                for (int i = 1; i < results.Count; i++)
                {
                    var lastSentence = results[i - 1].Result.Sentences.Last();
                    offset += (lastSentence.GraphemeLength + 
                        lastSentence.GraphemeOffset);
                    var currentSentences = results[i].Result.Sentences;
                    foreach (var item in currentSentences)
                    {
                        var newSentence = JObject.FromObject(item);
                        int offsetStart = newSentence.Value<int>("GraphemeOffset");
                        newSentence["GraphemeOffset"] = offsetStart + offset;
                        summarySentences.Add(newSentence);
                    }
                }
                cs["Sentences"] = summarySentences;
                try
                {
                    
                    ds = ToDocumentSentiment((JObject)cs);
                }
                catch (Exception)
                {

                    //throw;
                }
            }
            return ds;
        }


    }
}
