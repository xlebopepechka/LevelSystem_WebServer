namespace LevelSystem_WebServer.Models
{
    public class StatUpdateResult
    {
        public bool Success { get; set; }
        public int ExperienceGained { get; set; }
        public bool LeveledUp { get; set; }
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
        public int CurrentExperience { get; set; }
        public int NeededExperience { get; set; }
    }
}
