using Codingstube.Enums;
using Codingstube.Services;
using Discord.Interactions;

namespace Codingstube.Modules {
    public class CommandModule : InteractionModuleBase<SocketInteractionContext> {

        public InteractionService? Commands { get; set; }

        private InteractionHandler _handler;
        private CustomCommandService _commandService;

        public CommandModule(InteractionHandler handler, CustomCommandService commandService) {
            _handler = handler;
            _commandService = commandService;
        }

        [SlashCommand("command", "Bearbeite Befehle.")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task CommandAsync(
            CommandEnum option,
            [Summary(description: "Der Name des Befehls.")] string cmdName = "",
            [Summary(description: "Die Antwort des Bots auf diesen Befehl.")] string cmdAnswer = ""
        ) {


            //get client for later use
            var client = _handler.GetClient();

            switch (option) {
                case CommandEnum.Add:
                    if(cmdName == string.Empty) {
                        await Context.Interaction.RespondAsync("Der Befehl konnte nicht hinzugefügt werden, da kein Parameter für den Namen angegeben wurde.", ephemeral: true);
                        return;
                    }

                    if (cmdAnswer == string.Empty) {
                        await Context.Interaction.RespondAsync("Der Befehl konnte nicht hinzugefügt werden, da kein Parameter für die Antwort angegeben wurde.", ephemeral: true);
                        return;
                    }

                    if(await TryAddCommandAsync(cmdName, cmdAnswer)) {
                        //command added successfully
                        await Context.Interaction.RespondAsync($"Der Befehl **/{cmdName}** wurde hinzugefügt und kann nun verwendet werden.", ephemeral: true);
                    } else {
                        //command name already taken
                        await Context.Interaction.RespondAsync($"Der Befehl /{cmdName} existiert bereits. Bitte verwende einen anderen Namen oder entferne den Befehl zunächst.", ephemeral: true);
                    }
                    break;


                case CommandEnum.Remove:
                    if (cmdName == string.Empty) {
                        await Context.Interaction.RespondAsync("Der Befehl konnte nicht entfernt werden, da kein Parameter für den Namen angegeben wurde.", ephemeral: true);
                        return;
                    }

                    if (await TryRemoveCommandAsync(cmdName)) {
                        //command removed successfully
                        await Context.Interaction.RespondAsync($"Der Befehl **/{cmdName}** wurde entfernt.", ephemeral: true);
                    } else {
                        //couldn't find command with that name
                        await Context.Interaction.RespondAsync($"Der Befehl /{cmdName} existiert nicht und konnte daher nicht entfernt werden.", ephemeral: true);
                    }
                    break;


                case CommandEnum.List:
                    await Context.Interaction.RespondAsync("**Befehle:** \n" + GetCommandString(), ephemeral: true);
                    break;

                default:
                    await Context.Interaction.RespondAsync("Ungültige Option", ephemeral: true);
                    break;
            }

        }

        private async Task<bool> TryAddCommandAsync(string cmdName, string cmdResponse) {
            return await _commandService.AddCustomCommand(new CustomCommand() { Name = cmdName, Answer = cmdResponse });
        }

        private async Task<bool> TryRemoveCommandAsync(string cmdName) {

            //get cmd from command service
            CustomCommand? cmd = _commandService.GetCustomCommandByName(cmdName);
            if (cmd == null) return false;

            return await _commandService.RemoveCustomCommand(cmd);
        }

        private string GetCommandString() {

            //get cmds from command service
            List<CustomCommand> cmds = _commandService.GetCustomCommands();

            //generate string
            string cmdString = "";
            foreach(CustomCommand cmd in cmds) {
                cmdString += $"{cmd.Name} - {cmd.Answer}\n";
            }

            return cmdString;
        }
    }
}
