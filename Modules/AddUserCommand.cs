using Codingstube.Database;
using Codingstube.Database.Models;
using Discord;
using Discord.Interactions;

namespace Codingstube.Modules {
    public class AddUserCommand : InteractionModuleBase<SocketInteractionContext> {

        public InteractionService? Commands { get; set; }

        private InteractionHandler _handler;

        private readonly DatabaseContext _dbContext;

        public AddUserCommand(InteractionHandler handler, DatabaseContext dbContext) {
            _handler = handler;
            _dbContext = dbContext;

        }

        [SlashCommand("adduser", "Fügt ein User der Datenbank hinzu")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task WelcomeCommandAsync(
            [Summary(description: "Der User welcher in die Datenbank aufgenommen werden soll.")] IGuildUser user) {
            


            if (user == null) {
                await Context.Interaction.RespondAsync("Fehler bei der Ausführung.", ephemeral: true);
                LogMessage log = new(LogSeverity.Info, "WelcomeModule", $"Failed to get user.");
                await _handler.LogAsync(log);
                return;
            }

            //new user
            UserEntity newUser = new(user.Id, user.DisplayName, DateTimeOffset.Now);

            try {
                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();

                await Context.Interaction.RespondAsync("Willkommensnachricht wurde erfolgreich erstellt.", ephemeral: true);

            } catch (Exception ex) {
                LogMessage log = new(LogSeverity.Info, "WelcomeModule", ex.Message);
                await _handler.LogAsync(log);
            }

            

            
        }
    }
}
