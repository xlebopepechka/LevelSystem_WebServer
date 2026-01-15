using LevelSystem_WebServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LevelSystem_WebServer.Data
{
    public class ApplicationDbContextFactory
    {
        private readonly IConfiguration _configuration;

        public ApplicationDbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ApplicationDbContext CreateDbContext(string serverPort)
        {
            var connectionString = _configuration.GetConnectionString($"Server_{serverPort}")
                                ?? _configuration.GetValue<string>($"ServerConnections:{serverPort}");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException($"No connection string found for server port: {serverPort}");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}