using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ToDo.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        //services.AddDbContext<ToDoDbContext>(options =>
        //{
        //    options
        //        .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        //        .UseSnakeCaseNamingConvention();
        //});

        //services.AddScoped(provider =>
        //    provider.GetRequiredService<ToDoDbContext>());

        return services;
    }
}
