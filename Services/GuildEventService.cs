using Codingstube.Database;
using Codingstube.Database.Models;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Codingstube.Services {
    public class GuildEventService {

        private readonly DiscordSocketClient _client;
        private readonly InteractionHandler _handler;
        private readonly DatabaseContext _dbContext;

        public GuildEventService(DiscordSocketClient client, InteractionHandler handler, DatabaseContext dbContext) {
            _client = client;
            _handler = handler;
            _dbContext = dbContext;

            _client.UserJoined += OnUserJoinedGuildAsync;
            _client.UserLeft += OnUserLeftGuildAsync;
            _client.GuildMemberUpdated += OnGuildMemberUpdatedAsync;
        }

        private async Task OnUserJoinedGuildAsync(SocketGuildUser user) {

            //log join in event channel
            var embedBuilder = new EmbedBuilder()
                .WithAuthor(_client.CurrentUser.Username, _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl())
                .WithTitle("User joined")
                .WithDescription($"{user} hat den Server betreten.")
                .WithCurrentTimestamp();

            await _handler.WriteInEventChannelAsync(embed: embedBuilder.Build());

            //check if user is in database
            var userEntity = _dbContext.Users.FirstOrDefault(u => u.DiscordId == user.Id);

            if(userEntity != null) {
                //user already registered
            }
            else {
                //new user
                UserEntity newUser = new(user.Id, user.DisplayName, DateTimeOffset.Now);

                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();
            }

            //update bot status
            await _handler.UpdateGameStatus();
        }

        private async Task OnUserLeftGuildAsync(SocketGuild guild, SocketUser user) {

            //prepare builder for later
            var embedBuilder = new EmbedBuilder();

            //check if user got banned
            var ban = await guild.GetBanAsync(user);
            if (ban != null) {
                //user got banned
                var auditLog = await GetUserBanAuditLogEntryAsync(guild, user.Id);
                if (auditLog != null) {

                    //build embed
                    List<EmbedFieldBuilder> fields = new() {
                        new EmbedFieldBuilder().WithName("Administrator").WithValue($"{auditLog.User.Username}").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Grund").WithValue($"{auditLog.Reason}").WithIsInline(true)
                    };

                    embedBuilder
                        .WithAuthor(_client.CurrentUser.Username, _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl())
                        .WithTitle("User banned")
                        .WithDescription($"{user} wurde gebannt.")
                        .WithCurrentTimestamp()
                        .WithFields(fields);
                }

            }
            else {
                //user left
                //build embed
                embedBuilder
                .WithAuthor(_client.CurrentUser.Username, _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl())
                .WithTitle("User left")
                .WithDescription($"{user} hat den Server verlassen.")
                .WithCurrentTimestamp();
            }

            //log left in event channel
            await _handler.WriteInEventChannelAsync(embed: embedBuilder.Build());

            //update bot status
            await _handler.UpdateGameStatus();
        }

        private async Task OnGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> cachedUser, SocketGuildUser user) {

            //get old user data
            var oldUser = await cachedUser.GetOrDownloadAsync();

            //make sure user is not null
            if(oldUser == null) {
                throw new ArgumentNullException(nameof(cachedUser));
            }

            //create basic embed structure
            EmbedBuilder builder = new EmbedBuilder()
                    .WithAuthor(_client.CurrentUser.Username, _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl())
                    .WithDescription($"<@{user.Id}> hat sein Profil aktualisiert!")
                    .WithImageUrl(user.GetAvatarUrl())
                    .WithColor(Color.Blue)
                    .WithCurrentTimestamp();

            //add avatar changed
            if (oldUser.GetAvatarUrl() != user.GetAvatarUrl()) {
                //avatar changed
                EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                    .WithName("Avatar")
                    .WithValue($"[AvatarOld]({oldUser.GetAvatarUrl()}) -> [AvatarNew]({user.GetAvatarUrl()})")
                    .WithIsInline(true);

                builder.AddField(fieldBuilder);
            }

            //add discriminator changes
            if (oldUser.Discriminator != user.Discriminator) {
                //discriminator changed (for ex: Flashrex#0001 -> Flashrex#0002
                EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                    .WithName("Discriminator")
                    .WithValue($"{oldUser.Discriminator} -> {user.Discriminator}")
                    .WithIsInline(true);

                builder.AddField(fieldBuilder);
            }

            //add name changes
            if(oldUser.DisplayName != user.DisplayName) {
                //displayName changes
                EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                    .WithName("Name")
                    .WithValue($"{oldUser.DisplayName} -> {user.DisplayName}")
                    .WithIsInline(true);

                builder.AddField(fieldBuilder);
            }

            //nothing changed that the want to log
            if (builder.Fields.Count == 0) return;

            //send to event channel
            await _handler.WriteInEventChannelAsync(embed: builder.Build());
        }

        /// <summary>
        /// Extracts the last AuditLogEntry with actionType.Ban and the given userId
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task<RestAuditLogEntry?> GetUserBanAuditLogEntryAsync(SocketGuild guild, ulong userId) {
            var audit = await guild.GetAuditLogsAsync(1, actionType: ActionType.Ban, userId: userId).FlattenAsync();

            return audit.FirstOrDefault();
        }
    }
}
