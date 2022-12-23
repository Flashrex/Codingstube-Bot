using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;

namespace Codingstube.Modules {
    public class WelcomeModule : InteractionModuleBase<SocketInteractionContext> {

        public InteractionService? Commands { get; set; }

        private InteractionHandler _handler;
        public WelcomeModule(InteractionHandler handler) {
            _handler = handler;

            _handler.GetClient().ButtonExecuted += OnButtonExecuted;
        }

        [SlashCommand("welcome", "Erstellt die Willkommensnachricht und die dazugehörigen Buttons")]
        public async Task WelcomeCommandAsync(
            [Summary(description: "Die Id des Channels in der die Willkommensnachricht erstellt werden soll.")] IChannel channel) 
        {
            //get client for later use
            var client = _handler.GetClient();

            //get configuration
            var config = _handler.GetConfiguration();

            //get guild from client
            ulong guildid = config.GetValue<ulong>("guild");
            var guild = client.GetGuild(guildid);

            if (guild == null) {
                await Context.Interaction.RespondAsync("Fehler bei der Ausführung.", ephemeral: true);
                LogMessage log = new(LogSeverity.Info, "WelcomeModule", $"Failed to get guild with id {guildid}.");
                await _handler.LogAsync(log);
                return;
            }

            //build embed
            var embedBuilder = new EmbedBuilder()
                .WithAuthor(client.CurrentUser.Username, client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
                .WithTitle($"Willkommen auf dem {guild.Name} Discord.")
                .WithDescription("Wähle deine Plattform/-en um die Channel sehen zu können.")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();

            //create components (buttons)
            var builder = new ComponentBuilder()
                .WithButton("alt:V", "btn_altv")
                .WithButton("Rage:MP", "btn_ragemp")
                .WithButton("FiveM", "btn_fivem");

            //convert channel so we can write in it
            IMessageChannel mChannel = (IMessageChannel)channel;

            //create welcome message
            await mChannel.SendMessageAsync(embed: embedBuilder.Build(), components: builder.Build());

            await Context.Interaction.RespondAsync("Willkommensnachricht wurde erfolgreich erstellt.", ephemeral: true);
        }

        private async Task OnButtonExecuted(SocketMessageComponent component) {

            //convert user for later use
            if (component.User is not SocketGuildUser gUser) {
                await component.RespondAsync("Fehler bei der Ausführung.", ephemeral: true);
                LogMessage log = new(LogSeverity.Info, "WelcomeModule", $"Error while converting SocketUser to SocketGuildUser.");
                await _handler.LogAsync(log);
                return;
            }

            //check if user finished rule screening
            if (gUser.IsPending.GetValueOrDefault()) {
                await component.RespondAsync("Du musst zunächst das Regel Screening beenden um deine Plattform/-en wählen zu können.", ephemeral: true);
                return;
            }

            bool success = false;
            switch (component.Data.CustomId) {
                case "btn_altv":
                    success = await OnWelcomeMessageButtonPressedAsync(component.Data.CustomId, gUser);
                    break;

                case "btn_ragemp":
                    success = await OnWelcomeMessageButtonPressedAsync(component.Data.CustomId, gUser);
                    break;

                case "btn_fivem":
                    success = await OnWelcomeMessageButtonPressedAsync(component.Data.CustomId, gUser);
                    break;
            }

            //send info message
            await component.RespondAsync(success ? "Deine Rolle wurde hinzugefügt." : "Du hast diese Rolle bereits.", ephemeral: true);
        }

        private async Task<bool> OnWelcomeMessageButtonPressedAsync(string btnName, SocketGuildUser user) {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var client = _handler.GetClient();

            //get configuration
            var config = _handler.GetConfiguration();

            //get guild from client
            ulong guildid = config.GetValue<ulong>("guild");
            SocketGuild guild = client.GetGuild(guildid);

            //assign user role
            ulong userRoleId = config.GetValue<ulong>("userrole");

            SocketRole? userRole = guild.GetRole(userRoleId);
            if (userRole != null && !user.Roles.Contains(userRole)) {
                await user.AddRoleAsync(userRoleId);
            }

            //get roleid for chosen platform
            ulong roleId = 0;

            //reminder to update following code
            Console.WriteLine("Todo: Implement Plattform role system");

            //get platform roleid
            switch (btnName) {
                case "btn_altv":
                    roleId = 798410834023940106;
                    break;

                case "btn_ragemp":
                    roleId = 798410834023940106;
                    break;

                case "btn_fivem":
                    roleId = 798410834023940106;
                    break;
            }

            //assign plattform role
            SocketRole? role = guild.GetRole(roleId);
            if (role != null && !user.Roles.Contains(role)) {
                await user.AddRoleAsync(role);
                return true;
            } else {
                return false;
            }
        }
    }
}
