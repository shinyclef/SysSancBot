using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SysSancBot.DTO;
using SysSancBot.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SysSancBot
{
    public class Program
    {
        public const string CommandPrefix = "!ss ";
        private const string ConfigFileName = "config.json";

        public static ConfigDto Config { get; private set; }
        public static UserCredential credential { get; private set; }

        public static void Main(string[] args)
        {
            if (!TryLoadConfig())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The file 'config.json' could not be loaded. A default file has been placed next to the executable. Configure it and then run again. You can delete it to force a new templte to generate.");
                Console.ReadKey();
                return;
            }

            try
            {
                new Program().MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ReadKey();
                return;
            }
        }

        private static bool TryLoadConfig()
        {
            string fileLocation = System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase;
            if (fileLocation.ToLower().StartsWith(@"file:///"))
            {
                fileLocation = fileLocation.Substring(@"file:///".Length);
            }

            string rootPath = new FileInfo(fileLocation).Directory.ToString() + "\\";
            string path = new FileInfo(fileLocation).Directory.ToString() + "\\" + ConfigFileName;

            if (File.Exists(path))
            {
                try
                {
                    Config = JsonConvert.DeserializeObject<ConfigDto>(File.ReadAllText(path));
                }
                catch (Exception e)
                {
                    return false;
                }

                return true;
            }
            else
            {
                Config = new ConfigDto();
                string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(path, json);
                return false;
            }
        }

        public async Task MainAsync()
        {
            var services = ConfigureServices();
            DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();
            client.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            await client.LoginAsync(TokenType.Bot, Config.DiscordToken);
            await client.StartAsync();
            await services.GetRequiredService<CommandListener>().InitializeAsync();
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity, 8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();
            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandListener>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .AddSingleton<IDataService>(new GoogleSheetDataService())
                .AddSingleton<PluralService>()
                .BuildServiceProvider();
        }
    }
}