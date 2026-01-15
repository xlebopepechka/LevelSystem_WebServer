using LevelSystem_WebServer.Data;

namespace LevelSystem_WebServer.Models
{
    public class Achievement
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public StatType RequiredStat { get; set; }
        public int RequiredValue { get; set; }
        public bool IsHidden { get; set; } = false;
    }

    public class PlayerAchievement
    {
        public required string PlayerId { get; set; }
        public required string AchievementId { get; set; }
        public int CurrentProgress { get; set; } = 0;
        public bool IsUnlocked { get; set; } = false;
        public DateTime? CompletedAt { get; set; } = null;
    }
}
