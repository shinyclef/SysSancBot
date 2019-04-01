using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using SysSancBot.Enums;
using SysSancBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysSancBot.Modules
{
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly IDataService data;
        private Dictionary<string, ChannelRole> channels { get { return data.GetChannels(); } }

        public AdminModule(IServiceProvider services)
        {
            data = services.GetRequiredService<IDataService>();
        }

        [Command("ReloadTriggers")]
        [Summary("Reloads the trigger word list.")]
        public Task ReloadTriggers()
        {
            ChannelRole role;
            if (!channels.TryGetValue(Context.Channel.Name, out role) || role != ChannelRole.Admin)
            {
                return Task.CompletedTask;
            }

            MessageScanner.Instance.ReloadTriggerLists();
            return ReplyAsync("`The trigger word list has been reloaded.`");
        }

        [Command("ReloadChannels")]
        [Summary("Reloads the channel types.")]
        public Task ReloadChannels()
        {
            ChannelRole role;
            if (!channels.TryGetValue(Context.Channel.Name, out role) || role != ChannelRole.Admin)
            {
                return Task.CompletedTask;
            }

            MessageScanner.Instance.ReloadChannelTypes();
            return ReplyAsync("`The channel config list has been reloaded.`");
        }
    }
}