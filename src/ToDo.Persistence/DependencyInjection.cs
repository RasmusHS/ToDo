using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToDo.Persistence.Seeding;

namespace ToDo.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        bool seedDevData = bool.TryParse(configuration["Database:SeedDevData"], out var seed) && seed;

        services.AddDbContext<ToDoDbContext>(options =>
        {
            options
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention();

            if (seedDevData)
            {
                options.UseAsyncSeeding((ctx, _, ct) => DevDataSeeder.SeedAsync(ctx, ct));
                options.UseSeeding((ctx, _) => DevDataSeeder.Seed(ctx));
            }
        });

        return services;
    }
}
