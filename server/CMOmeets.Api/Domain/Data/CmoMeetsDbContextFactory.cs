using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CMOmeets.Domain.Data;

/// Used only by the EF Core CLI tooling at design time.
public class CmoMeetsDbContextFactory : IDesignTimeDbContextFactory<CmoMeetsDbContext>
{
    public CmoMeetsDbContext CreateDbContext(string[] args)
    {
        // Keep design-time migrations in sync with the running app: read the same
        // appsettings.json connection string. An env-var override still wins for one-off targets.
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = Environment.GetEnvironmentVariable("CMOMEETS_CONNECTION")
            ?? config.GetConnectionString("CMOmeets")
            ?? "Server=.;Database=CMOmeetsDB;Integrated Security=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<CmoMeetsDbContext>()
            .UseSqlServer(connection)
            .Options;

        return new CmoMeetsDbContext(options);
    }
}
