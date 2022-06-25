using Microsoft.EntityFrameworkCore;

namespace WeatherProject.Models
{
    public class WeatherContext : DbContext
    {
        private readonly IConfiguration _config;
        public WeatherContext(IConfiguration config)
        {
            _config = config;
        }
        public DbSet<Weather> Weathers { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer(_config.GetConnectionString("WeatherContext"), builder =>
            {
                builder.EnableRetryOnFailure();
            });
        }
    }
}
