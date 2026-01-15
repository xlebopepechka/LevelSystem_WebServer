namespace LevelSystem_WebServer.Models
{
    public class Player
    {
        public required string Id { get; set; }
        public required string Nickname { get; set; }
        public string DiscordId { get; set; } = "0";

        // Kills and Deaths
        public int HumanKills { get; set; } = 0;
        public int ScpKills { get; set; } = 0;
        public int ScpKilled { get; set; } = 0;
        public int Deaths { get; set; } = 0;

        // Misc
        public int Escapes { get; set; } = 0;
        public long BestEscapeTime { get; set; } = 0;
        public int MedicamentsUsed { get; set; } = 0;
        public int CandiesEaten { get; set; } = 0;
        public int Scp207Used { get; set; } = 0;
        public int RoundsPlayed { get; set; } = 0;
        public long SecondsPlayed { get; set; } = 0;

        // Level
        public int Level { get; set; } = 0;
        public int CurrentExperience { get; set; } = 0;
        public int NeededExperience { get; set; } = 100;
    }
}