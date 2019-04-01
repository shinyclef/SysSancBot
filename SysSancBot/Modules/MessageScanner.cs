using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SysSancBot.DTO;
using SysSancBot.Enums;
using SysSancBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysSancBot.Modules
{
    public class MessageScanner : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient discord;
        private readonly IDataService data;
        private readonly StemmingService stemSrv;

        private StringBuilder sb;
        private char[] trimChars;

        private HashSet<string> monitoredChannels;
        private HashSet<string> adminChannels;

        private Dictionary<string, ChannelRole> channels { get { return data.GetChannels(); } }
        private Dictionary<string, TriggerData> triggerWords { get { return data.GetTriggerWords(); } }

        public static MessageScanner Instance { get; private set; }

        public MessageScanner(IServiceProvider services)
        {
            discord = services.GetRequiredService<DiscordSocketClient>();
            data = services.GetRequiredService<IDataService>();
            stemSrv = services.GetRequiredService<StemmingService>();

            sb = new StringBuilder();
            trimChars = new char[] { '.', ',', '?', '!', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}' };

            ReloadChannelTypes();
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
            data.GetTriggerWords(true);
        }

        public void ReloadChannelTypes()
        {
            data.GetChannels(true);
            monitoredChannels = new HashSet<string>();
            adminChannels = new HashSet<string>();

            foreach (KeyValuePair<string, ChannelRole> pair in channels)
            {
                if (pair.Value == ChannelRole.Safe)
                {
                    monitoredChannels.Add(pair.Key);
                }
                else if (pair.Value == ChannelRole.Admin)
                {
                    adminChannels.Add(pair.Key);
                }
            }
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

            string channelName = msg.Channel.Name;
            SanitisedMsgResult result;
            if (monitoredChannels.Contains(channelName))
            {
                var sw = new Stopwatch();
                sw.Start();
                result = SanatiseMessage(msg);
                
                sw.Stop();
                if (result == null || result.Action == TriggerAction.None)
                {
                    return;
                }

                SocketGuildUser user = context.Guild.GetUser(discord.CurrentUser.Id);
                if (result.Action == TriggerAction.Inform)
                {
                    await SendAdminAlert();
                }
                else if (result.Action == TriggerAction.Censor)
                {
                    await SendAdminAlert();
                    await msg.Channel.DeleteMessageAsync(msg);
                    await user.ModifyAsync(x => x.Nickname = msg.Author.Username);
                    await context.Channel.SendMessageAsync(result.GetReplyMsg(context));
                    await user.ModifyAsync(x => x.Nickname = context.Client.CurrentUser.Username);
                }
            }

            async Task SendAdminAlert()
            {
                foreach (string adminChannel in adminChannels)
                {
                    SocketTextChannel channel = context.Guild.TextChannels.FirstOrDefault(c => c.Name == adminChannel);
                    if (channel != null)
                    {
                        await channel.SendMessageAsync(null, false, result.GetAdminEmbed());
                    }
                }
            }
        }

        private SanitisedMsgResult SanatiseMessage(SocketUserMessage msg)
        {
            
            string[] lines = msg.Content.Split('\n');
            bool triggerWasFound = false;

            TriggerAction maxAction = TriggerAction.None;
            HashSet<string> topics = null;

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

                    TriggerData data;
                    if (triggerWords.TryGetValue(lower, out data) || triggerWords.TryGetValue(stemSrv.GetStem(lower), out data))
                    {
                        maxAction = (TriggerAction)Math.Max((int)maxAction, (int)data.Action);
                        if (topics == null)
                        {
                            topics = new HashSet<string>();
                        }

                        topics.Add(data.Category);

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

            SanitisedMsgResult result = null;
            if (triggerWasFound)
            {
                result = new SanitisedMsgResult()
                {
                    Msg = msg,
                    Sanitised = triggerWasFound ? sb.ToString() : null,
                    Action = maxAction,
                    Topics = topics,
                };
            }
            
            sb.Clear();
            return result;
        }

        private class SanitisedMsgResult
        {
            public SocketUserMessage Msg;
            public string Sanitised;
            public HashSet<string> Topics;
            public TriggerAction Action;

            public string TopicsString { get { return string.Join(", ", Topics); } }

            public string GetReplyMsg(SocketCommandContext context)
            {
                return $"TW: {string.Join(", ", Topics)}\n{Sanitised}\n\n`Added the TW for you. ~ {context.Client.CurrentUser.Username}`";
            }

            public Embed GetAdminEmbed()
            {
                var builder = new EmbedBuilder()
                    .AddField($"Topics:", TopicsString, false)
                    .AddField($"{Msg.Author.Username} said:", Sanitised, false)
                    .AddField("Jump:", Msg.GetJumpUrl(), false)
                    .WithTitle(Action == TriggerAction.Inform ? "FYI, thought you might like to know?" : "Heads up, I censored something!")
                    .WithColor(Action == TriggerAction.Inform ? Color.Green : Color.Orange);
                return builder.Build();
            }
        }
    }
}