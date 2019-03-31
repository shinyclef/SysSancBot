using PluralizationService;
using PluralizationService.English;
using System.Globalization;

namespace SysSancBot.Services
{
    public class PluralService
    {
        private readonly IPluralizationApi Api;
        private readonly CultureInfo CultureInfo;

        public PluralService()
        {

            var builder = new PluralizationApiBuilder();
            builder.AddEnglishProvider();

            Api = builder.Build();
            CultureInfo = new CultureInfo("en-US");
        }

        public string Pluralize(string name)
        {
            return Api.Pluralize(name, CultureInfo) ?? name;
        }

        public string Singularize(string name)
        {
            return Api.Singularize(name, CultureInfo) ?? name;
        }
    }
}