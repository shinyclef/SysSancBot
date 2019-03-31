using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SysSancBot.Services
{
    public class CommandListener
    {
        private readonly CommandService commands;
        private readonly DiscordSocketClient discord;
        private readonly IServiceProvider services;

        public CommandListener(IServiceProvider services)
        {
            this.services = services;
            discord = services.GetRequiredService<DiscordSocketClient>();
            
            //commands = services.GetRequiredService<CommandService>();
            commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false,
                IgnoreExtraArgs = false
            });

            commands.CommandExecuted += CommandExecutedAsync;
            discord.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, and non-user messages
            SocketUserMessage msg = rawMessage as SocketUserMessage;
            if (msg == null || msg.Source != MessageSource.User)
            {
                return;
            }

            var context = new SocketCommandContext(discord, msg);
            var argPos = 0; // This value holds the offset where the prefix ends
            if (msg.HasStringPrefix(Program.CommandPrefix, ref argPos))
            {
                await commands.ExecuteAsync(context, argPos, services); // we will handle the result in CommandExecutedAsync,        
            }
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if command is unspecified when there was a search failure (command not found), or command was successful, we don't care
            if (!command.IsSpecified || result.IsSuccess)
            {
                return;
            }
                
            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}