using Discord.Commands;
using System.Threading.Tasks;

namespace SysSancBot.Modules
{
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("ReloadTriggerLists")]
        [Summary("Reloads the trigger lists.")]
        public Task ReloadTriggerLists()
        {
            MessageScanner.Instance.ReloadTriggerLists();
            return ReplyAsync("The trigger lists have been reloaded.");
        }
    }
}