using LevelSystem_WebServer.Data;
using LevelSystem_WebServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LevelSystem_WebServer.Services
{
    public class AchievementsService
    {
        private readonly IConfiguration _configuration;
        private readonly List<Achievement> _achievements;

        public AchievementsService(ApplicationDbContextFactory dbFactory, IConfiguration configuration)
        {
            _configuration = configuration;
            _achievements = _configuration.GetSection("Achievements")
                .Get<List<Achievement>>() ?? new List<Achievement>();
        }

        public async Task InitializePlayerAchievements(ApplicationDbContext context, string playerId)
        {
            var existingCount = await context.PlayerAchievements
                .CountAsync(pa => pa.PlayerId == playerId);

            if (existingCount > 0) 
                return;

            foreach (var achievement in _achievements)
            {
                var playerAchievement = new PlayerAchievement
                {
                    PlayerId = playerId,
                    AchievementId = achievement.Id,
                    CurrentProgress = 0,
                    IsUnlocked = false,
                    CompletedAt = null
                };
                context.PlayerAchievements.Add(playerAchievement);
            }

            await context.SaveChangesAsync();
        }


        public async Task CheckAndAddMissingAchievements(ApplicationDbContext context, string playerId)
        {
            var playerAchievementIds = await context.PlayerAchievements
                .Where(pa => pa.PlayerId == playerId)
                .Select(pa => pa.AchievementId)
                .ToListAsync();

            var missingAchievements = _achievements
                .Where(a => !playerAchievementIds.Contains(a.Id))
                .ToList();

            foreach (var achievement in missingAchievements)
            {
                var newPlayerAchievement = new PlayerAchievement
                {
                    PlayerId = playerId,
                    AchievementId = achievement.Id,
                    CurrentProgress = 0,
                    IsUnlocked = false,
                    CompletedAt = null
                };
                context.PlayerAchievements.Add(newPlayerAchievement);
            }

            if (missingAchievements.Count > 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"Добавлено {missingAchievements.Count} новых ачивок игроку {playerId}");
            }
        }

        public async Task CheckAchievements(Player player, StatType updatedStat, ApplicationDbContext context)
        {
            var relevantAchievements = _achievements
                .Where(a => a.RequiredStat == updatedStat)
                .ToList();

            foreach (var achievement in relevantAchievements)
            {
                var playerAchievement = await context.PlayerAchievements
                    .FirstOrDefaultAsync(pa => pa.PlayerId == player.Id && pa.AchievementId == achievement.Id);

                if (playerAchievement == null)
                    return;

                if (!playerAchievement.IsUnlocked)
                {
                    int currentStatValue = GetPlayerStatValue(player, updatedStat);
                    playerAchievement.CurrentProgress = Math.Min(currentStatValue, achievement.RequiredValue);

                    if (currentStatValue >= achievement.RequiredValue)
                    {
                        playerAchievement.IsUnlocked = true;
                        playerAchievement.CompletedAt = DateTime.UtcNow;
                        Console.WriteLine($"Игрок {player.Nickname} получил ачивку: {achievement.Name}");
                    }
                }
            }
        }

        private static int GetPlayerStatValue(Player player, StatType statType)
        {
            return statType switch
            {
                StatType.HumanKills => player.HumanKills,
                StatType.ScpKills => player.ScpKills,
                StatType.ScpKilled => player.ScpKilled,
                StatType.Deaths => player.Deaths,
                StatType.Escapes => player.Escapes,
                StatType.MedicamentsUsed => player.MedicamentsUsed,
                StatType.CandiesEaten => player.CandiesEaten,
                StatType.Scp207Used => player.Scp207Used,
                StatType.RoundsPlayed => player.RoundsPlayed,
                StatType.SecondsPlayed => (int)player.SecondsPlayed,
                _ => 0
            };
        }
    }
}
