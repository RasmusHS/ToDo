using Microsoft.Extensions.DependencyInjection;
using ToDo.Application.CQRS.Commands.ToDoItem;
using ToDo.Application.CQRS.Commands.ToDoItem.Handlers;
using ToDo.Application.CQRS.Commands.ToDoList;
using ToDo.Application.CQRS.Commands.ToDoList.Handlers;
using ToDo.Application.CQRS.Queries.ToDoList;
using ToDo.Application.CQRS.Queries.ToDoList.Handlers;
using ToDo.Application.Profiles;

namespace ToDo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

        //services.AddScoped<, >();

        // ToDoList
        services.AddScoped<ICreateToDoListCommand, CreateToDoListCommandHandler>();
        services.AddScoped<IGetToDoListQuery, GetToDoListQueryHandler>();

        // ToDoItem
        services.AddScoped<ICreateToDoItemCommand, CreateToDoItemCommandHandler>();

        return services;
    }
}
