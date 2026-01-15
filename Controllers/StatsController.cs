using LevelSystem_WebServer.Data;
using LevelSystem_WebServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace LevelSystem_WebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly StatsService _statsService;
        private readonly ILogger<StatsController> _logger;

        public StatsController(StatsService statsService, ILogger<StatsController> logger)
        {
            _statsService = statsService;
            _logger = logger;
        }

        [HttpGet("player")]
        public async Task<IActionResult> GetPlayerStats([FromQuery] string Id, [FromQuery] string serverPort)
        {
            var player = await _statsService.GetPlayerStats(Id, serverPort);
            return player == null ? NotFound() : Ok(player);
        }

        [HttpPost("player")]
        public async Task<IActionResult> CreatePlayer([FromQuery] string Id, [FromQuery] string nickname, [FromQuery] string serverPort)
        {
            var player = await _statsService.GetOrCreatePlayer(Id, nickname, serverPort);
            return CreatedAtAction(nameof(GetPlayerStats), new { Id, serverPort }, player);
        }

        [HttpPost("player/reset-stats")]
        public async Task<IActionResult> ResetPlayerStats([FromQuery] string Id, [FromQuery] string serverPort)
        {
            var playerStatsReseted = await _statsService.ResetPlayerStats(Id, serverPort);
            return playerStatsReseted ? Ok() : NotFound();
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddStat([FromQuery] string Id, [FromQuery] StatType type,
            [FromQuery] int value = 1, [FromQuery] float multiplier = 1f, [FromQuery] string serverPort = "")
        {
            var result = await _statsService.AddStat(Id, type, value, multiplier, serverPort);
            return Ok(result);
        }

        [HttpPost("addtoall")]
        public async Task<IActionResult> AddStatToAll([FromBody] Dictionary<string, string> Ids, [FromQuery] StatType type, [FromQuery] int value = 1, [FromQuery] string serverPort = "")
        {
            await _statsService.AddStatToAll(Ids, type, value, serverPort);
            return Ok();
        }

        [HttpPost("discord/store-code")]
        public IActionResult StoreDiscordCode([FromQuery] string code, [FromQuery] string discordId, [FromQuery] string serverPort)
        {
            try
            {
                var expiry = DateTime.UtcNow.AddMinutes(5);
                DiscordLinkService.AddCode(code, discordId, expiry, serverPort);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing Discord code {Code}", code);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("discord/validate-code")]
        public async Task<IActionResult> ValidateDiscordCode([FromQuery] string code, [FromQuery] string playerId, [FromQuery] string serverPort = "")
        {
            _logger.LogInformation($"ValidateDiscordCode called: code={code}, playerId={playerId}");

            try
            {
                var result = await _statsService.LinkWithCode(code, playerId, serverPort);
                if (result)
                {
                    _logger.LogInformation($"Code validated successfully: {code}");
                    return Ok(new { Success = true, Message = "Discord успешно привязан!" });
                }
                else
                {
                    _logger.LogWarning($"Invalid or expired code: {code}");
                    return BadRequest("Invalid or expired code provided");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Discord code {Code}", code);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("discord/is-linked")]
        public async Task<IActionResult> IsDiscordLinked([FromQuery] string discordId, [FromQuery] string serverPort = "")
        {
            var isLinked = await _statsService.IsDiscordLinked(discordId, serverPort);
            return isLinked == "notfound" ? NotFound() : Ok(isLinked);
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard([FromQuery] StatType type, [FromQuery] string serverPort)
        {
            var leaderboard = await _statsService.GetLeaderboard(type, serverPort);
            return leaderboard == null ? NotFound() : Ok(leaderboard);
        }
    }
}