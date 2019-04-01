using PluralizationService;
using PluralizationService.English;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace SysSancBot.Services
{
    public class StemmingService
    {
        private const string LemmaFileName = "lemmatization-en.txt";
        private Dictionary<string, string> wordForms;

        private readonly IPluralizationApi Api;
        private readonly CultureInfo CultureInfo;

        public StemmingService()
        {
            BuildStemmingDictionary();

            var builder = new PluralizationApiBuilder();
            builder.AddEnglishProvider();

            Api = builder.Build();
            CultureInfo = new CultureInfo("en-US");
        }

        public string GetStem(string word)
        {
            string stem;
            if (!wordForms.TryGetValue(word, out stem))
            {
                stem = word;
            }

            return stem;
        }
        
        public string Pluralize(string word)
        {
            return Api.Pluralize(word, CultureInfo) ?? word;
        }

        public string Singularize(string word)
        {
            return Api.Singularize(word, CultureInfo) ?? word;
        }

        private void BuildStemmingDictionary()
        {
            wordForms = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(LemmaFileName);
            for (int i = 0; i < lines.Length; i++)
            {
                int tabIndex = lines[i].IndexOf('\t');
                string form = lines[i].Substring(tabIndex + 1);
                if (!wordForms.ContainsKey(form))
                {
                    wordForms.Add(form.ToLower(), lines[i].Substring(0, tabIndex).ToLower());
                }
            }
        }
    }
}