using Codingstube.Modules;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Codingstube.Services {
    public class CustomCommand {
        public string Name { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class CustomCommandService {

        public List<CustomCommand> Commands { get; set; } = new();

        private InteractionHandler _handler;
        private CommandFileService _fileService;

        public CustomCommandService(InteractionHandler handler, CommandFileService fileService) {
            _handler = handler;
            _fileService = fileService;

            _handler.GetClient().SlashCommandExecuted += OnCustomCommandAsync;
        }

        public async Task LoadCustomCommandsAsync() {

            //load cmds from file
            Commands = await _fileService.LoadCommandsFromFileAsync("", "commands.json");

            //register cmds to guild
            foreach (CustomCommand command in Commands) {

                if (!await RegisterCommandToGuildAsync(command)) {
                    LogMessage msg = new(LogSeverity.Error, "CustomCommandService", $"Failed to register command with name {command.Name}.");
                    await _handler.LogAsync(msg);
                }
            }

            //logging
            LogMessage log = new(LogSeverity.Info, "CustomCommandService", $"Loaded {Commands.Count} Commands from file.");
            await _handler.LogAsync(log);
        }

        public async Task<bool> AddCustomCommand(CustomCommand cmd) {
            if (Commands.Contains(cmd)) return false;

            //add cmd to list
            Commands.Add(cmd);

            //overwrite command file
            await _fileService.OverWriteCommandFileAsync("", "commands.json", Commands);

            //register cmd to guild
            return await RegisterCommandToGuildAsync(cmd);
        }

        private async Task<bool> RegisterCommandToGuildAsync(CustomCommand cmd) {

            //get client and config
            DiscordSocketClient client = _handler.GetClient();
            var config = _handler.GetConfiguration();

            //get guild from client
            ulong guildid = config.GetValue<ulong>("guild");

            var guild = client.GetGuild(guildid);
            if (guild == null) {
                LogMessage log = new(LogSeverity.Info, "CustomCommandService", $"Failed to get guild with id {guildid}.");
                await _handler.LogAsync(log);
                return false;
            }

            //create command builder
            var commandBuilder = new SlashCommandBuilder()
                .WithName(cmd.Name)
                .WithDescription("Antwortet mit " + cmd.Answer)
                .WithDefaultMemberPermissions(GuildPermission.UseApplicationCommands);

            //try registering command to guild
            try {
                await guild.CreateApplicationCommandAsync(commandBuilder.Build());
                return true;

            } catch (Exception ex) {
                LogMessage msg = new(LogSeverity.Error, ex.Source, ex.Message);
                await _handler.LogAsync(msg);
                return false;
            }
        }

        public async Task<bool> RemoveCustomCommand(CustomCommand cmd) {
            if (!Commands.Contains(cmd)) return false;

            //remove cmd from list
            Commands.Remove(cmd);

            //overwrite command file
            await _fileService.OverWriteCommandFileAsync("", "commands.json", Commands);

            //remove cmd from guild
            return await RemoveCommandFromGuildAsync(cmd);
        }

        private async Task<bool> RemoveCommandFromGuildAsync(CustomCommand cmd) {

            //get client and config
            DiscordSocketClient client = _handler.GetClient();
            var config = _handler.GetConfiguration();

            //get guild from client
            var guild = client.GetGuild(config.GetValue<ulong>("guild"));
            if (guild == null) return false;

            //get guild commands
            var cmds = await guild.GetApplicationCommandsAsync();

            //check if command is registered
            var command = cmds.SingleOrDefault(x => x.Name == cmd.Name);

            //no command with this name registered
            if (command == null) return false;

            //delete command
            await command.DeleteAsync();
            return true;
        }

        public CustomCommand? GetCustomCommandByName(string name) {
            return Commands.FirstOrDefault(c => c.Name == name);
        }

        public List<CustomCommand> GetCustomCommands() {
            return Commands;
        }

        public async Task OnCustomCommandAsync(SocketSlashCommand cmd) {
            //get command from list
            CustomCommand? command = GetCustomCommandByName(cmd.Data.Name);

            //get client
            var client = _handler.GetClient();

            if (command != null) {
                //create embed
                var embedBuilder = new EmbedBuilder()
                        .WithAuthor(client.CurrentUser.Username, client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
                        .WithTitle(command.Name)
                        .WithDescription(command.Answer)
                        .WithColor(Color.Blue)
                        .WithCurrentTimestamp();

                try {
                    //send response
                    await cmd.RespondAsync(embed: embedBuilder.Build());
                } catch (Exception ex) {
                    LogMessage msg = new(LogSeverity.Error, ex.Source, ex.Message);
                    await _handler.LogAsync(msg);
                }
            }
        }
    }
}
