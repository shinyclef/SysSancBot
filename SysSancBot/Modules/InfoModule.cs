﻿using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SysSancBot.Services;
using System;
using System.Threading.Tasks;

namespace SysSancBot.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly StemmingService pluralSrv;

        public InfoModule(IServiceProvider services)
        {
            pluralSrv = services.GetRequiredService<StemmingService>();
        }

        [Command("Help")]
        public Task Help()
        {
            string msg = "```Commands:\n" +
                "- ReloadTriggers: Reloads the trigger word list.\n" +
                "- ReloadChannels: Reloads the channel types.\n" +
                "```";
            return ReplyAsync(msg);
        }

        [Command("Singular")]
        public Task Singular([Remainder] [Summary("The word for which you want the singular form.")] string word)
        {
            return ReplyAsync($"I think the singular form of **{word.ToLower()}** is **{pluralSrv.Singularize(word.ToLower())}**.");
        }

        [Command("Plural")]
        public Task Plural([Remainder] [Summary("The word for which you want the plural form.")] string word)
        {
            return ReplyAsync($"I think the plural form of **{word.ToLower()}** is **{pluralSrv.Pluralize(word.ToLower())}**.");
        }

        [Command("Stem")]
        public Task Stem([Remainder] [Summary("The word for which you want the stem.")] string word)
        {
            return ReplyAsync($"I think the stem of **{word.ToLower()}** is **{pluralSrv.GetStem(word.ToLower())}**.");
        }

        [Command("SetName")]
        [Summary("Set's the bot's name, theoreetically....")]
        public Task SetName([Remainder] [Summary("Name to set the bot to.")] string newName)
        {
            return Context.Client.CurrentUser.ModifyAsync(x => x.Username = newName);
        }

        [Command("You")]
        public Task You()
        {
            return ReplyAsync(Context.Client.CurrentUser.Username);
        }

        [Command("say")]
        [Summary("Echoes a message.")]
        public Task SayAsync([Remainder] [Summary("The text to echo")] string msg)
        {
            return ReplyAsync(msg);
        }
    }

    // Create a module with the 'sample' prefix
    [Group("sample")]
    public class SampleModule : ModuleBase<SocketCommandContext>
    {
        // ~sample square 20 -> 400
        [Command("square")]
        [Summary("Squares a number.")]
        public async Task SquareAsync([Summary("The number to square.")] int num)
        {
            // We can also access the channel from the Command Context.
            await Context.Channel.SendMessageAsync($"{num}^2 = {num * num}");
        }

        // ~sample userinfo --> foxbot#0282
        // ~sample userinfo @Khionu --> Khionu#8708
        // ~sample userinfo Khionu#8708 --> Khionu#8708
        // ~sample userinfo Khionu --> Khionu#8708
        // ~sample userinfo 96642168176807936 --> Khionu#8708
        // ~sample whois 96642168176807936 --> Khionu#8708
        [Command("userinfo")]
        [Summary
        ("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync([Summary("The (optional) user to get info from")] SocketUser user = null)
        {
            SocketUser userInfo = user ?? Context.Client.CurrentUser;
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
        }
    }
}
