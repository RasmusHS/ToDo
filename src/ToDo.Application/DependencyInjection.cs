using Microsoft.Extensions.DependencyInjection;
using ToDo.Application.Profiles;

namespace ToDo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

        //services.AddScoped<, >();
        //services.AddScoped<ICreateToDoItemCommand, CreateToDoItemCommandHandler>();

        return services;
    }
}
