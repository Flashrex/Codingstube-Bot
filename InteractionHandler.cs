using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System;
using System.Threading.Tasks;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;
using System.Diagnostics.Tracing;

namespace Codingstube {
    public class InteractionHandler {

        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;

        public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services, IConfiguration config) {
            _client = client;
            _handler = handler;
            _services = services;
            _configuration = config;
        }

        public IConfiguration GetConfiguration() {
            return _configuration;
        }

        public async Task InitializeAsync() {

            // Process when the client is ready, so we can register our commands.
            _client.Ready += ReadyAsync;
            _handler.Log += LogAsync;

            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += HandleInteraction;
        }

        private async Task LogAsync(LogMessage message) {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"[{DateTime.Now.ToLongTimeString()}] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{message.Source}] {message.Message}\n");
        } 

        public async Task WriteInEventChannelAsync(string? text = null, Embed? embed = null) {

            var channelId = _configuration.GetValue<ulong>("eventchannel");

            IChannel? channel = await _client.GetChannelAsync(channelId) as SocketChannel;

            if (channel != null) {
                IMessageChannel mChannel = (IMessageChannel)channel;
                await mChannel.SendMessageAsync(text: text, embed: embed);
            }
        }

        private async Task ReadyAsync() {
            // Context & Slash commands can be automatically registered, but this process needs to happen after the client enters the READY state.
            // Since Global Commands take around 1 hour to register, we should use a test guild to instantly update and test our commands.
            if (Program.IsDebug())
                await _handler.RegisterCommandsToGuildAsync(_configuration.GetValue<ulong>("codingstube"), true);
            else
                await _handler.RegisterCommandsGloballyAsync(true);


            //getting current guild
            var guild = _client.GetGuild(_configuration.GetValue<ulong>("codingstube"));

            //set game status
            await _client.SetGameAsync($"Verwalte {guild.MemberCount} Member auf dem {guild.Name} Discord.");
        }

        private async Task HandleInteraction(SocketInteraction interaction) {
            try {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                //Execute the incoming command.
                var result = await _handler.ExecuteCommandAsync(context, _services);

                if(!result.IsSuccess) {
                    switch(result.Error) {
                        case InteractionCommandError.UnmetPrecondition:
                            //todo
                            break;

                        default:
                            break;
                    }
                }
            } catch {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if(interaction.Type is InteractionType.ApplicationCommand) {
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
                }
            }
        }
    }
}
