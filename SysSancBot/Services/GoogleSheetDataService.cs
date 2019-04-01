using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using SysSancBot.Common;
using SysSancBot.DTO;
using SysSancBot.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SysSancBot.Services
{
    public class GoogleSheetDataService : IDataService
    {
        private const string credentialJson = "googlesheets.json";
        private const string triggerListRange = "TriggerList";
        private const string channelListRange = "ChannelList";

        private Dictionary<string, TriggerData> triggerWords;
        private Dictionary<string, ChannelRole> channels;

        private UserCredential credential;

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        private string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private string ApplicationName = "SysSancBot";
        private string spreadsheetId;
        private SheetsService service;

        public GoogleSheetDataService()
        {
            SetupConnection();
        }

        private void SetupConnection()
        {
            spreadsheetId = Program.Config.GoogleSheetId;
            using (var stream =new FileStream(credentialJson, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Google Sheets API service.
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public Dictionary<string, TriggerData> GetTriggerWords(bool forceReload = false)
        {
            if (!forceReload && triggerWords != null)
            {
                return triggerWords;
            }

            var result = new Dictionary<string, TriggerData>();
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, triggerListRange);
            ValueRange response = request.Execute();
            IList<IList<Object>> rows = response.Values;
            if (rows != null && rows.Count > 0)
            {
                for (int i = 1; i < rows.Count; i++)
                {
                    if (rows[i].Count > 0)
                    {
                        string word = rows[i][0].ToString().ToLower();
                        string category = rows[i].Count > 1 ? rows[i][1].ToString().ToLower() : null;
                        string type = rows[i].Count > 2 ? rows[i][2].ToString().ToLower() : null;
                        if (!string.IsNullOrWhiteSpace(word))
                        {
                            result.Add(word, new TriggerData() { Category = category, Action = Util.GetTriggerActionFromString(type) });
                        }
                    }
                }
            }

            if (result.Count == 0)
            {
                Console.WriteLine("Warning, no trigger words found.");
            }


            triggerWords = result;
            return result;
        }

        public Dictionary<string, ChannelRole> GetChannels(bool forceReload = false)
        {
            if (!forceReload && channels != null)
            {
                return channels;
            }

            var result = new Dictionary<string, ChannelRole>();
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, channelListRange);
            ValueRange response = request.Execute();
            IList<IList<Object>> rows = response.Values;
            if (rows != null && rows.Count > 0)
            {
                for (int i = 1; i < rows.Count; i++)
                {
                    if (rows[i].Count > 0)
                    {
                        string channel = rows[i][0].ToString().ToLower();
                        string type = rows[i].Count > 1 ? rows[i][1].ToString().ToLower() : null;
                        if (!string.IsNullOrWhiteSpace(channel))
                        {
                            result.Add(channel, Util.GetChannelTypeFromString(type));
                        }
                    }
                }
            }

            if (result.Count == 0)
            {
                Console.WriteLine("Warning, no channel list found.");
            }

            channels = result;
            return result;
        }
    }
}
