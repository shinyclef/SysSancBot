using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SysSancBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SysSancBot.Modules
{
    public class MessageScanner : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient discord;
        private readonly IDataService data;
        private readonly PluralService pluralSrv;

        private StringBuilder sb;
        private char[] trimChars;

        private HashSet<string> simpleWords;

        public static MessageScanner Instance { get; private set; }

        public MessageScanner(IServiceProvider services)
        {
            discord = services.GetRequiredService<DiscordSocketClient>();
            data = services.GetRequiredService<IDataService>();
            pluralSrv = services.GetRequiredService<PluralService>();

            sb = new StringBuilder();
            trimChars = new char[] { '.', ',', '?', '!', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}' };

            ReloadTriggerLists();
            discord.MessageReceived += ProcessMessage;
            Instance = this;
        }

        public void ReloadTriggerLists()
        {
            ReloadSimpleWords();
        }

        public void ReloadSimpleWords()
        {
            simpleWords = data.GetSimpleWords();
        }

        private async Task ProcessMessage(SocketMessage rawMsg)
        {
            // Ignore system messages, and non-user messages
            SocketUserMessage msg = rawMsg as SocketUserMessage;
            if (msg == null || msg.Source != MessageSource.User)
            {
                return;
            }

            var context = new SocketCommandContext(discord, msg);
            var argPos = 0; // This value holds the offset where the prefix ends
            if (msg.HasStringPrefix(Program.CommandPrefix, ref argPos))
            {
                return;
            }

            if (msg.Channel.Name == "bot-testing")
            {
                var sw = new Stopwatch();
                sw.Start();
                string content = SanatiseMessage(msg.Content);
                sw.Stop();
                if (content == null)
                {
                    return;
                }

                await msg.Channel.DeleteMessageAsync(msg);

                string author = msg.Author.Username;
                SocketGuildUser user = context.Guild.GetUser(discord.CurrentUser.Id);
                await user.ModifyAsync(x => x.Nickname = author);
                await context.Channel.SendMessageAsync($"```Orig: {msg.Content}\nEdit: {content}\nI took {sw.Elapsed.TotalMilliseconds}ms.```");
                //await context.Channel.SendMessageAsync(content);
                await user.ModifyAsync(x => x.Nickname = context.Client.CurrentUser.Username);
            }
        }

        private string SanatiseMessage(string msg)
        {
            string[] lines = msg.Split('\n');
            bool triggerWasFound = false;
            for (int l = 0; l < lines.Length; l++)
            {
                if (l > 0)
                {
                    sb.Append('\n');
                }

                string[] words = lines[l].Split(' ');
                bool prevWasTrigger = false;
                string prevSuffix = string.Empty;
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i].TrimEnd(trimChars);
                    string suffix = words[i].Substring(word.Length);
                    string lower = word.ToLower();
                    if (simpleWords.Contains(lower) || simpleWords.Contains(pluralSrv.Singularize(lower)))
                    {
                        if (i != 0)
                        {
                            sb.Append(prevSuffix).Append(' ');
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
                            sb.Append(prevSuffix).Append(' ');
                        }

                        sb.Append(word);
                        prevWasTrigger = false;
                    }

                    prevSuffix = suffix;
                }

                if (prevWasTrigger)
                {
                    sb.Append("||");
                }

                sb.Append(prevSuffix);
            }

            string result = triggerWasFound ? sb.ToString() : null;
            sb.Clear();
            return result;
        }
    }
}