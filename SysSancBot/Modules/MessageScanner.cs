using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SysSancBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SysSancBot.Modules
{
    public class MessageScanner : ModuleBase<SocketCommandContext>
    {
        private readonly IServiceProvider services;
        private readonly DiscordSocketClient discord;
        private StringBuilder sb;
        private char[] trimChars;

        public MessageScanner(IServiceProvider services)
        {
            this.services = services;
            discord = services.GetRequiredService<DiscordSocketClient>();
            services.GetRequiredService<MessageListener>().OnMsgReceived += OnMessageReceived;
            sb = new StringBuilder();
            trimChars = new char[] { '.', ',', '?', '!', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}' };
        }

        private void OnMessageReceived(SocketMessage msg, SocketCommandContext context)
        {
            ProcessMessage(msg, context).GetAwaiter().GetResult();
        }

        private async Task ProcessMessage(SocketMessage msg, SocketCommandContext context)
        {
            if (msg.Channel.Name == "bot-testing")
            {
                string content = SanatiseMessage(msg.Content);
                if (content == null)
                {
                    return;
                }

                await msg.Channel.DeleteMessageAsync(msg);

                string author = msg.Author.Username;
                SocketGuildUser user = context.Guild.GetUser(discord.CurrentUser.Id);
                await user.ModifyAsync(x => x.Nickname = author);
                await context.Channel.SendMessageAsync($"```Orig: {msg.Content}\nEdit: {content}```");
                await user.ModifyAsync(x => x.Nickname = context.Client.CurrentUser.Username);
            }
        }

        private string SanatiseMessage(string msg)
        {
            var triggers = new HashSet<string>() { "pillow", "one", "two", "three" };
            string[] words = msg.Split(' ');
            bool prevWasTrigger = false;
            bool triggerWasFound = false;
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (triggers.Contains(word.Trim(trimChars).ToLower()))
                {
                    if (i != 0)
                    {
                        sb.Append(" ");
                    }

                    if (!prevWasTrigger)
                    {
                        sb.Append("||");
                    }

                    sb.Append(word);
                    prevWasTrigger = true;
                    triggerWasFound = true;
                }
                else
                {
                    if (prevWasTrigger)
                    {
                        sb.Append("||");
                    }

                    if (i != 0)
                    {
                        sb.Append(" ");
                    }

                    sb.Append(word);
                    prevWasTrigger = false;
                }
            }

            if (prevWasTrigger)
            {
                sb.Append("||");
            }

            string result = triggerWasFound ? sb.ToString() : null;
            sb.Clear();
            return result;
        }
    }
}