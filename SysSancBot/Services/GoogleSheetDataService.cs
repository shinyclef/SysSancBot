using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SysSancBot.Services
{
    public class GoogleSheetDataService : IDataService
    {
        private const string credentialJson = "googlesheets.json";
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

        public HashSet<string> GetSimpleWords()
        {
            var set = new HashSet<string>();

            // Define request parameters.
            String range = "SimpleWordsList";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row.Count > 0)
                    {
                        string val = row[0].ToString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            set.Add(val);
                        }
                    }
                }
            }

            if (set.Count == 0)
            {
                Console.WriteLine("Warning, no \"Simple Words\" found.");
            }

            return set;
        }
    }
}
