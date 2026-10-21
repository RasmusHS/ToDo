using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ToDo.Persistence;

namespace ToDo.Api.Utilities;

[ExcludeFromCodeCoverage]
public static class MigrationExtensions
{
    public static void ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();

        dbContext.Database.Migrate();
    }
}
