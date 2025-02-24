using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

var connectionString = Environment.GetEnvironmentVariable("ConnectionString");

var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();

optionsBuilder.UseNpgsql(connectionString);
optionsBuilder.UseOpenIddict();
var context = new ApplicationContext(optionsBuilder.Options);

await context.Database.MigrateAsync();

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
{
    public ApplicationContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
        
        var connectionString = args[0];
        
        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.UseOpenIddict();
        return new ApplicationContext(optionsBuilder.Options);
    }
}