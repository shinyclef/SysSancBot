using SysSancBot.DTO;
using SysSancBot.Enums;
using System.Collections.Generic;

namespace SysSancBot.Services
{
    public interface IDataService
    {
        Dictionary<string, TriggerData> GetTriggerWords(bool forceReload = false);

        Dictionary<string, ChannelRole> GetChannels(bool forceReload = false);
    }
}
