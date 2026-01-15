using LevelSystem_WebServer.Data;
using LevelSystem_WebServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LevelSystem_WebServer.Services
{
    public class StatsService
    {
        private readonly ApplicationDbContextFactory _dbFactory;
        private readonly ILogger<StatsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly AchievementsService _achievementsService;

        public StatsService(ApplicationDbContextFactory dbFactory, ILogger<StatsService> logger, 
            IConfiguration configuration, AchievementsService achievementsService)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _configuration = configuration;
            _achievementsService = achievementsService;
        }

        public async Task<StatUpdateResult> AddStat(string playerId, StatType type, int value = 1, float multiplier = 1f, string serverPort = "")
        {
            if (value <= 0) return new StatUpdateResult { Success = false };

            try
            {
                using var context = _dbFactory.CreateDbContext(serverPort);
                var player = await GetOrCreatePlayer(context, playerId);

                var oldLevel = player.Level;
                var result = ApplyStatUpdate(player, type, value, multiplier);

                await _achievementsService.CheckAchievements(player, type, context);
                await context.SaveChangesAsync();

                result.Success = true;
                result.OldLevel = oldLevel;
                result.NewLevel = player.Level;
                result.CurrentExperience = player.CurrentExperience;
                result.NeededExperience = player.NeededExperience;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding stat for player {PlayerId} on server {Server}", playerId, serverPort);
                return new StatUpdateResult { Success = false };
            }
        }

        public async Task AddStatToAll(Dictionary<string, string> playerIds, StatType type, int value = 1, string serverPort = "")
        {
            if (playerIds == null || playerIds.Count == 0) return;

            try
            {
                using var context = _dbFactory.CreateDbContext(serverPort);

                foreach (var playerId in playerIds)
                {
                    var player = await GetOrCreatePlayerInternal(context, playerId.Key, playerId.Value);
                    ApplyStatUpdate(player, type, value, 1f);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding stat to multiple players on server {Server}", serverPort);
            }
        }

        public async Task<Player?> GetPlayerStats(string playerId, string serverPort)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext(serverPort);
                return await context.Players.FindAsync(playerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for player {PlayerId} on server {Server}", playerId, serverPort);
                return null;
            }
        }

        public async Task<Player> GetOrCreatePlayer(string playerId, string nickname, string serverPort)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext(serverPort);
                return await GetOrCreatePlayer(context, playerId, nickname);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting/creating player {PlayerId} on server {Server}", playerId, serverPort);
                throw;
            }
        }

        private async Task<Player> GetOrCreatePlayer(ApplicationDbContext context, string playerId, string? nickname = null)
        {
            var player = await context.Players.FindAsync(playerId);
            if (player != null)
            {
                await _achievementsService.CheckAndAddMissingAchievements(context, playerId);

                if (nickname != null && player.Nickname != nickname)
                {
                    player.Nickname = nickname;
                    Console.WriteLine("Changed Uknown name in the DB");
                }
                return player;
            }

            player = new Player
            {
                Id = playerId,
                Nickname = nickname ?? "Unknown",
            };

            context.Players.Add(player);
            await context.SaveChangesAsync();

            await _achievementsService.InitializePlayerAchievements(context, playerId);

            return player;
        }

        private async Task<Player> GetOrCreatePlayerInternal(ApplicationDbContext context, string playerId, string? nickname = null)
        {
            var player = await context.Players.FindAsync(playerId);
            if (player != null)
            {
                if (nickname != null && player.Nickname != nickname)
                {
                    player.Nickname = nickname;
                    Console.WriteLine("Changed Uknown name in the DB");
                }
                return player;
            }

            player = new Player
            {
                Id = playerId,
                Nickname = nickname ?? "Unknown",
            };

            context.Players.Add(player);
            return player;
        }

        public async Task<bool> ResetPlayerStats(string playerId, string serverPort)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext(serverPort);
                var player = await context.Players.FindAsync(playerId);
                if (player == null)
                    return false;

                var defaultPlayer = new Player
                {
                    Id = player.Id,
                    Nickname = player.Nickname,
                    DiscordId = player.DiscordId,
                    Level = 0,
                    NeededExperience = CalculateNeededExperience(0)
                };

                context.Entry(player).CurrentValues.SetValues(defaultPlayer);
                //await ResetPlayerAchievements(context, playerId);

                await context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting stats for player {PlayerId} on server {Server}", playerId, serverPort);
                return false;
            }
        }

        private StatUpdateResult ApplyStatUpdate(Player player, StatType type, int value, float multiplier)
        {
            var result = new StatUpdateResult();
            var expRewards = _configuration.GetSection("ExperienceRewards");
            var experienceGained = 0;

            switch (type)
            {
                case StatType.HumanKills:
                    player.HumanKills += value;
                    experienceGained = expRewards.GetValue<int>("HumanKills");
                    break;
                case StatType.ScpKills:
                    player.ScpKills += value;
                    experienceGained = expRewards.GetValue<int>("ScpKills");
                    break;
                case StatType.ScpKilled:
                    player.ScpKilled += value;
                    experienceGained = expRewards.GetValue<int>("ScpKilled");
                    break;
                case StatType.Deaths:
                    player.Deaths += value;
                    experienceGained = expRewards.GetValue<int>("Deaths");
                    break;
                case StatType.Escapes:
                    player.Escapes += value;
                    experienceGained = expRewards.GetValue<int>("Escapes");
                    break;
                case StatType.BestEscapeTime:
                    if (value > 0 && (player.BestEscapeTime == 0 || value < player.BestEscapeTime))
                    {
                        player.BestEscapeTime = value;
                    }
                    break;
                case StatType.MedicamentsUsed:
                    player.MedicamentsUsed += value;
                    experienceGained = expRewards.GetValue<int>("MedicamentsUsed");
                    break;
                case StatType.CandiesEaten:
                    player.CandiesEaten += value;
                    experienceGained = expRewards.GetValue<int>("CandiesEaten");
                    break;
                case StatType.Scp207Used:
                    player.Scp207Used += value;
                    experienceGained = expRewards.GetValue<int>("Scp207Used");
                    break;
                case StatType.RoundsPlayed:
                    player.RoundsPlayed += value;
                    experienceGained = expRewards.GetValue<int>("RoundsPlayed");
                    break;
                case StatType.SecondsPlayed:
                    player.SecondsPlayed += value;
                    experienceGained = expRewards.GetValue<int>("SecondsPlayed");
                    break;
                case StatType.Experience:
                    experienceGained = value;
                    break;
                default:
                    _logger.LogWarning("Unknown stat type: {StatType}", type);
                    break;
            }

            if (experienceGained > 0)
            {
                var actualExperience = (int)(experienceGained * multiplier);
                player.CurrentExperience += actualExperience;
                result.ExperienceGained = actualExperience;
                result.LeveledUp = CheckLevelUp(player);
            }

            return result;
        }

        private bool CheckLevelUp(Player player)
        {
            var leveledUp = false;
            while (player.CurrentExperience >= player.NeededExperience)
            {
                player.Level++;
                player.CurrentExperience -= player.NeededExperience;
                player.NeededExperience = CalculateNeededExperience(player.Level);
                leveledUp = true;
            }
            return leveledUp;
        }

        private int CalculateNeededExperience(int currentLevel)
        {
            var levelSystemConfig = _configuration.GetSection("LevelSystem");
            var baseExp = levelSystemConfig.GetValue<int>("NeededExperience");
            var multiplier = levelSystemConfig.GetValue<int>("NeededExperienceMultiplier");

            return baseExp + (currentLevel * multiplier);
        }

        public async Task<bool> LinkWithCode(string code, string playerId, string serverPort)
        {
            var discordInfo = DiscordLinkService.GetDiscordInfoFromCode(code, serverPort);
            if (discordInfo == null)
                return false;

            using var context = _dbFactory.CreateDbContext(serverPort);
            var player = await context.Players.FindAsync(playerId);

            if (player == null)
                return false;

            player.DiscordId = discordInfo.DiscordId;
            await context.SaveChangesAsync();

            DiscordLinkService.RemoveCode(code, serverPort);
            return true;
        }

        public async Task<string> IsDiscordLinked(string discordId, string serverPort)
        {
            using var context = _dbFactory.CreateDbContext(serverPort);
            var player = await context.Players.FirstOrDefaultAsync(p => p.DiscordId == discordId);
            return player == null ? "notfound" : player.Id;
        }

        public async Task<List<LeaderboardEntry>> GetLeaderboard(StatType statType, string serverPort)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext(serverPort);

                IQueryable<Player> query = context.Players;

                if (statType.ToString().Equals("BestEscapeTime", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p => EF.Property<int>(p, statType.ToString()) != 0);
                }

                IOrderedQueryable<Player> orderedQuery;

                if (statType.ToString().Equals("BestEscapeTime", StringComparison.OrdinalIgnoreCase))
                {
                    orderedQuery = query.OrderBy(p => EF.Property<int>(p, statType.ToString()));
                }
                else
                {
                    orderedQuery = query.OrderByDescending(p => EF.Property<int>(p, statType.ToString()));
                }

                var leaderboardData = await orderedQuery
                    .Take(10)
                    .Select(p => new { p.Nickname, StatValue = EF.Property<int>(p, statType.ToString()) })
                    .ToListAsync();

                var leaderboard = new List<LeaderboardEntry>();
                foreach (var data in leaderboardData)
                {
                    leaderboard.Add(new LeaderboardEntry { Nickname = data.Nickname, StatValue = data.StatValue.ToString() });
                }

                return leaderboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leaderboard for stat type {StatType} on server {Server}", statType, serverPort);
                return new List<LeaderboardEntry>();
            }
        }

        public async Task InitializeAllPlayersAchievements(string serverPort)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext(serverPort);

                var playerIds = await context.Players.Select(p => p.Id).ToListAsync();
                _logger.LogInformation($"Found {playerIds.Count} players on server {serverPort}");

                var achievements = _configuration.GetSection("Achievements")
                    .Get<List<Achievement>>() ?? new List<Achievement>();

                int totalAdded = 0;

                foreach (var playerId in playerIds)
                {
                    var existingAchievements = await context.PlayerAchievements
                        .Where(pa => pa.PlayerId == playerId)
                        .Select(pa => pa.AchievementId)
                        .ToListAsync();

                    var missingAchievements = achievements
                        .Where(a => !existingAchievements.Contains(a.Id))
                        .ToList();

                    if (missingAchievements.Count > 0)
                    {
                        foreach (var achievement in missingAchievements)
                        {
                            context.PlayerAchievements.Add(new PlayerAchievement
                            {
                                PlayerId = playerId,
                                AchievementId = achievement.Id,
                                CurrentProgress = 0,
                                IsUnlocked = false,
                                CompletedAt = null
                            });
                        }
                        totalAdded += missingAchievements.Count;
                    }
                }

                if (totalAdded > 0)
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Added {totalAdded} achievement records for server {serverPort}");
                }
                else
                {
                    _logger.LogInformation($"All players already have achievements on server {serverPort}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initializing achievements for server {serverPort}");
                throw;
            }
        }

        public class LeaderboardEntry
        {
            public required string Nickname { get; set; }
            public required string StatValue { get; set; }
        }
    }
}