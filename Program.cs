using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Interactions;
using Codingstube.Database;
using Microsoft.EntityFrameworkCore;

namespace Codingstube {
    public class Program {

        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;

        private readonly DiscordSocketConfig _socketConfig = new() {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildEmojis | GatewayIntents.GuildMessages,
            AlwaysDownloadUsers = true
        };

        public Program() {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("settings.json")
                .Build();

            var conString = $"SERVER={_configuration["server"]};PORT={_configuration["port"]};DATABASE={_configuration["database"]};UID={_configuration["userid"]};PASSWORD={_configuration["password"]};";
            _services = ConfigureServices(_configuration, _socketConfig, conString);
        }

        private static IServiceProvider ConfigureServices(IConfiguration _configuration, DiscordSocketConfig _socketConfig, string _conString) {
            return new ServiceCollection()
                .AddSingleton(_configuration)
                .AddSingleton(_socketConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddDbContext<DatabaseContext>(
            options => options.UseMySql(_conString, ServerVersion.AutoDetect(_conString))
            )
            .BuildServiceProvider();
        }

        static void Main(string[] args) 
            => new Program().RunAsync()
            .GetAwaiter()
            .GetResult();

        public async Task RunAsync() {

            var client = _services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;

            // Here we can initialize the service that will register and execute our commands
            await _services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();

            // Bot token can be provided from the Configuration object we set up earlier
            await client.LoginAsync(TokenType.Bot, _configuration["token"]);
            await client.StartAsync();

            //Never terminate program until manually forced to.
            await Task.Delay(Timeout.Infinite);
        }

        private async Task LogAsync(LogMessage message) {
            await Task.Run(() => {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write($"[{DateTime.Now.ToLongTimeString()}] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"[{message.Source}] {message.Message}\n");
            });

        }
            

        public static bool IsDebug() {
            #if DEBUG
            return true;
            #else
            return false;
            #endif
        }
    }
}