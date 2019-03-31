using System.Collections.Generic;

namespace SysSancBot.Services
{
    public interface IDataService
    {
        HashSet<string> GetSimpleWords();
    }
}
