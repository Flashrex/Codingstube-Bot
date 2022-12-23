using System.ComponentModel.DataAnnotations.Schema;

namespace Codingstube.Database.Models {
    public class UserEntity {

        [NotMapped]
        const uint XP_PER_LEVEL = 250;

        public uint Id { get; set; }
        public ulong DiscordId { get; set; }
        public string Username { get; set; }
        public DateOnly Registered { get; set; }
        public uint MessagesSend { get; set; }
        public uint Level { get; set; }
        public uint XP_Total { get; set; }
        public uint XP { get; set; }


        public UserEntity() {
            Username = string.Empty;
        }

        public UserEntity(ulong discordId, string username, DateTimeOffset joined) {
            DiscordId = discordId;
            Username = username;
            Registered = DateOnly.FromDateTime(joined.Date);
            Level = 1;
        }

        public bool GiveXp(uint xp) {
            XP_Total += xp;
            XP += xp;

            if (XP >= Level * XP_PER_LEVEL) {
                //Level up
                XP -= Level * XP_PER_LEVEL;
                Level++;
                return true;
            }

            return false;
        }

        public uint GetXpNeeded() {
            return XP_PER_LEVEL * Level;
        }
    }
}
