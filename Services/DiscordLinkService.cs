using System.Collections.Concurrent;

namespace LevelSystem_WebServer.Services
{
    public static class DiscordLinkService
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DiscordLinkCode>> _linkCodes = new();

        public static void AddCode(string code, string discordId, DateTime expiry, string serverPort)
        {
            Console.WriteLine($"Adding {code}, {discordId}, {serverPort} to the LinkService");

            var serverCodes = _linkCodes.GetOrAdd(serverPort, _ => new ConcurrentDictionary<string, DiscordLinkCode>());
            var linkCode = new DiscordLinkCode
            {
                DiscordId = discordId,
                Expiry = expiry
            };

            if (serverCodes.TryAdd(code, linkCode))
            {
                Console.WriteLine($"SUCCESS: Code {code} for server {serverPort} successfully added.");
            }
            else
            {
                Console.WriteLine($"ERROR: Code {code} for server {serverPort} already exists. Addition failed.");
            }
        }

        public static DiscordLinkCode? GetDiscordInfoFromCode(string code, string serverPort)
        {
            if (_linkCodes.TryGetValue(serverPort, out var serverCodes))
            {
                if (serverCodes.TryGetValue(code, out var linkCode))
                {
                    if (linkCode.Expiry >= DateTime.UtcNow)
                    {
                        return linkCode;
                    }

                    serverCodes.TryRemove(code, out _);
                    Console.WriteLine($"Removed expired code: {code} from server: {serverPort}");
                }
            }
            return null;
        }

        public static void RemoveCode(string code, string serverPort)
        {
            if (_linkCodes.TryGetValue(serverPort, out var serverCodes))
            {
                if (serverCodes.TryRemove(code, out _))
                {
                    Console.WriteLine($"Successfully removed code: {code} from server: {serverPort}");
                }
            }
        }

        public class DiscordLinkCode
        {
            public string DiscordId { get; set; } = "0";
            public DateTime Expiry { get; set; }
        }
    }
}