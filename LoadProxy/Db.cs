using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LoadProxy
{
    public sealed class Db:DbContext
    {
        private readonly Configuration _configuration;
        public DbSet<Proxy> Proxies { get; set; }
        public Db()
        {
            _configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText("appsettings.json"));
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_configuration.Route);
        }   
    }
}