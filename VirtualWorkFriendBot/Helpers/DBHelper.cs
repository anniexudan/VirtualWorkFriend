using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualWorkFriendBot.Models;

namespace VirtualWorkFriendBot.Helpers
{
    public static class DBHelper
    {
        public enum ConnectionContext
        {
            Application,
            State
        }
        public static IConfiguration Configuration { get; set; }

        private static string GetConnectionString(ConnectionContext ctx)
        {
            var connectionString = String.Empty;
            switch (ctx)
            {
                case ConnectionContext.Application:
                    connectionString = Configuration.GetConnectionString("Application");
                    break;
                case ConnectionContext.State:
                    connectionString = Configuration["SQLBotState"];
                    break;
            }
            return connectionString;
        }

        private static List<StringBuilder> GetJSONResultsFromReader(SqlDataReader reader)
        {
            List<StringBuilder> results = new List<StringBuilder>();
            var result = new StringBuilder();
            if (!reader.HasRows)
            {
                result.Append("[]");
            }
            else
            {
                while (reader.Read())
                {
                    result.Append(reader.GetValue(0).ToString());
                }
            }
            results.Add(result);

            while (reader.NextResult())
            {
                result = new StringBuilder();
                if (!reader.HasRows)
                {
                    result.Append("[]");
                }
                else
                {
                    while (reader.Read())
                    {
                        result.Append(reader.GetValue(0).ToString());
                    }
                }
                results.Add(result);
            }
            return results;
        }

        private static void ExecuteSQLStoredProc(string procedureName, Action<SqlCommand> action,
            ConnectionContext ctx = ConnectionContext.Application)
        {
            try
            {
                string connString = GetConnectionString(ctx);
                using (var conn = new SqlConnection(connString))
                using (var command = new SqlCommand(procedureName, conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    conn.Open();
                    action(command);
                }
            }
            catch (SqlException ex)
            {
                Debug.Fail($"Database caught while executing: {procedureName}", ex.Message);
            }
        }

        #region State Members
        private static void AddStateParameters(SqlCommand command, BotStateContext context)
        {
            AddStateParameters(command, context.ContextId, context.Category, context.PropertyName);
        }
        private static void AddStateParameters(SqlCommand command,
            string contextId, BotStorageCategory category, string propertyName)
        {
            command.Parameters.Add("@contextId", SqlDbType.VarChar);
            command.Parameters["@contextId"].Value = contextId;
            if (String.IsNullOrEmpty(contextId))
            {
                command.Parameters["@contextId"].Value = DBNull.Value;
            }
            else
            {
                command.Parameters["@contextId"].Value = contextId;
            }

            command.Parameters.Add("@storageId ", SqlDbType.Int);
            command.Parameters["@storageId "].Value = category;

            command.Parameters.Add("@propertyName", SqlDbType.VarChar);
            command.Parameters["@propertyName"].Value = propertyName;
        }



        public static T GetStateObject<T>(BotStateContext context, Func<T> fnDefault) where T : class
        {
            T result = null;
            var procedureName = "GetStateObject";
            var dbOperation = new Action<SqlCommand>(command =>
            {
                // Do Something
                AddStateParameters(command, context);
                var r = command.ExecuteReader();

                var builders = GetJSONResultsFromReader(r);
                string data = builders[0].ToString();
                if (data != "[]")
                {
                    result = JsonConvert.DeserializeObject<T>(
                        data);
                }

                if (result == null)
                {
                    result = fnDefault();
                }
            });
            ExecuteSQLStoredProc(procedureName, dbOperation, ConnectionContext.State);
            return result;
        }
        public static void RemoveStateObject<T>(BotStateContext context) where T : class
        {

            var procedureName = "DeleteStateObject";
            var dbOperation = new Action<SqlCommand>(command =>
            {
                // Do Something
                AddStateParameters(command, context);
                command.ExecuteNonQuery();
            });
            ExecuteSQLStoredProc(procedureName, dbOperation, ConnectionContext.State);
        }
        public static void SaveStateObject<T>(BotStateContext context, T value) where T : class
        {

            var procedureName = "PersistStateObject";
            var dbOperation = new Action<SqlCommand>(command =>
            {
                // Do Something
                AddStateParameters(command, context);

                command.Parameters.Add("@state", SqlDbType.NVarChar);
                command.Parameters["@state"].Value = JsonConvert.SerializeObject(value);

                command.ExecuteNonQuery();
            });
            ExecuteSQLStoredProc(procedureName, dbOperation, ConnectionContext.State);
        }
        #endregion

        public static JournalPrompt GetJournalPrompt()
        {
            const int COLUMN_PROMPT = 0;
            const int COLUMN_DESCRIPTION = 1;
            const int COLUMN_SOURCE = 2;
            const int COLUMN_INDEX_SOURCE = 3;
            const int COLUMN_INDEX_PROMPT = 4;

            JournalPrompt result = null;
            var procedureName = "[Application].[GetRandomJournalPrompt]";
            var dbOperation = new Action<SqlCommand>(command =>
            {
                // Do Something                
                var r = command.ExecuteReader();
                if (r.Read())
                {
                    result = new JournalPrompt
                    {
                        Prompt = r.GetString(COLUMN_PROMPT),
                        Details = r.GetString(COLUMN_DESCRIPTION),
                        Source = r.GetString(COLUMN_SOURCE),

                        SourceIndex = r.GetInt32(COLUMN_INDEX_SOURCE),
                        PromptIndex = r.GetInt32(COLUMN_INDEX_PROMPT)
                    };
                }
            });
            ExecuteSQLStoredProc(procedureName, dbOperation, ConnectionContext.Application);
            return result;
        }

        public static void CreateJournalEntry(JournalEntry entry)
        {
            var procedureName = "[Application].[CreateJournalEntry]";
            var dbOperation = new Action<SqlCommand>(command =>
            {
                command.Parameters.AddWithValue("@EntryId", entry.Id);
                command.Parameters.AddWithValue("@ContextId", entry.UserContextId);

                command.Parameters.AddWithValue("@PromptSourceId", entry.PromptSourceId);
                command.Parameters.AddWithValue("@PromptIndexId", entry.PromptIndexId);
                command.Parameters.AddWithValue("@EntryDate", entry.EntryDate);

                command.ExecuteNonQuery();

            });
            ExecuteSQLStoredProc(procedureName, dbOperation, ConnectionContext.Application);
        }
        public static void RemoveInvalidJournalEntries(KeyValuePair<string, List<string>> invalidEntries)
        {
            var procedureName = "[Application].[RemoveInvalidJournalEntries]";
            var dbOperation = new Action<SqlCommand>(command =>
            {
                command.Parameters.AddWithValue("@ContextId", invalidEntries.Key);
                command.Parameters.AddWithValue("@EntryIds", JsonConvert.SerializeObject(invalidEntries.Value));

                command.ExecuteNonQuery();

            });
            ExecuteSQLStoredProc(procedureName, dbOperation, ConnectionContext.Application);
        }
        public static List<JournalEntry> GetJournalEntries(string contextId, DateTime entryStart, DateTime entryEnd)
        {
            const int COLUMN_ENTRYID = 0;
            const int COLUMN_PROMPTSOURCEID = 1;
            const int COLUMN_PROMPTINDEXID = 2;
            const int COLUMN_ENTRYDATE = 3;
            const int COLUMN_SENTIMENT = 4;
            const int COLUMN_LASTMODIFIED = 5;

            List<JournalEntry> result = new List<JournalEntry>();
            var procedureName = "[Application].[GetJournalEntries]";
            var dbOperation = new Action<SqlCommand>(command =>
            {
                command.Parameters.AddWithValue("@ContextId", contextId);

                command.Parameters.AddWithValue("@StartDate", entryStart);
                command.Parameters.AddWithValue("@EndDate", entryEnd);

                var r = command.ExecuteReader();
                while (r.Read())
                {
                    var je = new JournalEntry
                    {
                        UserContextId = contextId,
                        Id = r.GetString(COLUMN_ENTRYID),
                        PromptSourceId = r.GetInt32(COLUMN_PROMPTSOURCEID),
                        PromptIndexId = r.GetInt32(COLUMN_PROMPTINDEXID),
                        EntryDate = r.GetDateTime(COLUMN_ENTRYDATE)
                    };
                    if (!r.IsDBNull(COLUMN_SENTIMENT))
                    {
                        je.Sentiment = TextAnalyticsHelper.ToDocumentSentiment( 
                            r.GetString(COLUMN_SENTIMENT));
                    }
                    if (!r.IsDBNull(COLUMN_LASTMODIFIED))
                    {
                        je.LastModified = r.GetDateTime(COLUMN_LASTMODIFIED);
                    }
                    result.Add(je);
                }

            });
            ExecuteSQLStoredProc(procedureName, dbOperation, ConnectionContext.Application);
            return result;
        }
        public static void SaveJournalEntryAssessment(JournalEntryAssessment assessment)
        {
            var procedureName = "[Application].[UpdateJournalEntryAssessment]";
            var dbOperation = new Action<SqlCommand>(command =>
            {
                command.Parameters.AddWithValue("@EntryId", assessment.Id);
                command.Parameters.AddWithValue("@Sentiment", JsonConvert.SerializeObject( assessment.Sentiment));
                command.Parameters.AddWithValue("@LastModified", assessment.LastModified);

                command.ExecuteNonQuery();

            });
            ExecuteSQLStoredProc(procedureName, dbOperation, ConnectionContext.Application);
        }
    }
}
