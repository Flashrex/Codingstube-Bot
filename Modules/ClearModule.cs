using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Codingstube.Modules {
    public class ClearModule : InteractionModuleBase<SocketInteractionContext> {

        private const int MAX_CLEAR_MESSAGES = 20;
        public InteractionService? Commands { get; set; }

        private InteractionHandler _handler;

        public ClearModule(InteractionHandler handler) {
            _handler = handler;
        }

        [SlashCommand("clear", "Entfernt ein oder mehrere Nachrichten aus diesem Channel.")]
        public async Task ClearCommandAsync(
            [Summary(description: "Die Anzahl an Nachrichten, die gelöscht werden sollen."),
            MinValue(1),
            MaxValue(MAX_CLEAR_MESSAGES)] int anzahlNachrichten) 
        {

            //convert user so we can get his permissions
            if (Context.User is not IGuildUser user) return;

            //get user permissions
            var permissions = user.GetPermissions(Context.Channel as IGuildChannel);

            //check if user has permission to manage messages
            var embedBuilder = new EmbedBuilder();
            if (!permissions.ManageMessages) {
                
                //user is missing this permission
                embedBuilder
                   .WithAuthor(user)
                   .WithTitle("Ungenügende Rechte")
                   .WithDescription($"Du hast nicht die Berechtigung Nachrichten zu entfernen!")
                   .WithColor(Color.Blue)
                   .WithCurrentTimestamp();


                await Context.Interaction.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }

            //get last `anzahlNachrichten` messages
            var messages = await Context.Channel.GetMessagesAsync(anzahlNachrichten).FlattenAsync();

            //filter out messages that are older than 14 days (can't delete msg's that are 14 days or older)
            var filteredMessages = messages.Where(m => (DateTimeOffset.UtcNow - m.Timestamp).TotalDays <= 14);

            //convert channel
            SocketTextChannel? textChannel = Context.Channel as SocketTextChannel;
            if (textChannel == null) {
                Console.WriteLine("Bot is missing scope or access to retrieve channel in ClearModule");
                return;
            }

            //deleting messages
            await textChannel.DeleteMessagesAsync(filteredMessages);

            //sending answer to user
            embedBuilder
            .WithAuthor(user)
            .WithTitle("Nachrichten entfernt")
            .WithDescription($"Du hast {filteredMessages.Count()} Nachrichten entfernt.")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();
            await Context.Interaction.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);

            //writing in event channel
            embedBuilder = new EmbedBuilder()
                .WithAuthor(Context.Client.CurrentUser)
                .WithTitle("Nachrichten entfernt")
                .WithDescription($"<@{user.Id}> hat **{filteredMessages.Count()}** Nachricht/-en im Channel <#{Context.Channel.Id}> gelöscht.")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();

            await _handler.WriteInEventChannelAsync(embed: embedBuilder.Build());
        }
    }
}
