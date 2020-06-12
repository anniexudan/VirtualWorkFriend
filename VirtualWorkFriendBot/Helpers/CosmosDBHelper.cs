using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder.Azure;
using VirtualWorkFriendBot.Models;
using System.IO;
using System.Text;

namespace VirtualWorkFriendBot.Helpers
{
    public class CosmosDBHelper
    {
        private static string databaseId = "botstate-db";
        private static string containerId = "botstate-collection";
        private static readonly JsonSerializer Serializer = new JsonSerializer();

        //Reusable instance of ItemClient which represents the connection to a Cosmos endpoint
        private static Database database = null;
        private static Container container = null;
        private static CosmosClient client = null;

        public static void Initialize(CosmosDbStorageOptions options)
        {
            CosmosClient client = new CosmosClient(
                options.CosmosDBEndpoint.ToString(), 
                options.AuthKey);
            databaseId = options.DatabaseId;
            containerId = options.CollectionId;

            Task<DatabaseResponse> tConnect = client.CreateDatabaseIfNotExistsAsync(databaseId);
            //tConnect.Start();
            tConnect.Wait();

            database = tConnect.Result.Database;
            container =  database.GetContainer(containerId);
            //"/_partitionKey"
        }

        public async Task<OnboardingState> GetOnboardingState(string key)
        {
           OnboardingState value = null;
            // Read the same item but as a stream.
            using (ResponseMessage responseMessage = await container.ReadItemStreamAsync(
                partitionKey: new PartitionKey(),
                id: key))
            {
                // Item stream operations do not throw exceptions for better performance
                if (responseMessage.IsSuccessStatusCode)
                {
                    value = FromStream<OnboardingState>(responseMessage.Content);
                    Console.WriteLine($"\n1.2.2 - Item Read {value.SignedInUserId}");

                    // Log the diagnostics
                    Console.WriteLine($"\n1.2.2 - Item Read Diagnostics: {responseMessage.Diagnostics.ToString()}");
                }
                else
                {
                    Console.WriteLine($"Read item from stream failed. Status code: {responseMessage.StatusCode} Message: {responseMessage.ErrorMessage}");
                }
            }
            return value;
        }
        public async Task<bool> SetOnboardingState(string key, OnboardingState value)
        {
            bool persisted = false;
            //var response = await container.CreateItemAsync<OnboardingState>(value);
            using (Stream stream = ToStream<OnboardingState>(value))
            {
                using (ResponseMessage responseMessage = await container.CreateItemStreamAsync(stream,
                    new PartitionKey())) // new PartitionKey(value.SignedInUserId)))
                {
                    // Item stream operations do not throw exceptions for better performance
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        OnboardingState streamResponse = FromStream<OnboardingState>(responseMessage.Content);
                        Console.WriteLine($"\n1.1.2 - Item created {streamResponse.SignedInUserId}");
                    }
                    else
                    {
                        Console.WriteLine($"Create item from stream failed. Status code: {responseMessage.StatusCode} Message: {responseMessage.ErrorMessage}");
                    }
                    persisted = responseMessage.IsSuccessStatusCode;
                }
            }
            return persisted;
        }

        #region Stream Conversion
        private static T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                if (typeof(Stream).IsAssignableFrom(typeof(T)))
                {
                    return (T)(object)(stream);
                }

                using (StreamReader sr = new StreamReader(stream))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                    {
                        return Serializer.Deserialize<T>(jsonTextReader);
                    }
                }
            }
        }
        private static Stream ToStream<T>(T input)
        {
            MemoryStream streamPayload = new MemoryStream();
            using (StreamWriter streamWriter = new StreamWriter(streamPayload, encoding: Encoding.Default, bufferSize: 1024, leaveOpen: true))
            {
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    writer.Formatting = Newtonsoft.Json.Formatting.None;
                    Serializer.Serialize(writer, input);
                    writer.Flush();
                    streamWriter.Flush();
                }
            }

            streamPayload.Position = 0;
            return streamPayload;
        }
        #endregion
    }
}
