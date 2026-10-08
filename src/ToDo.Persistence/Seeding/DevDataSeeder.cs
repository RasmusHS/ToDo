using Microsoft.EntityFrameworkCore;
using ToDo.Domain;

namespace ToDo.Persistence.Seeding;

internal static class DevDataSeeder
{
    public static async Task SeedAsync(DbContext ctx, CancellationToken ct)
    {
        if (await ctx.Set<ToDoListEntity>().AnyAsync(ct)) return; // idempotent

        var groceries = new ToDoListEntity("Groceries", null);
        var chores = new ToDoListEntity("Chores", "Weekend stuff");

        ctx.Set<ToDoListEntity>().AddRange(groceries, chores);
        ctx.Set<ToDoItemEntity>().AddRange(
            new ToDoItemEntity(groceries.Id, "Milk", null),
            new ToDoItemEntity(groceries.Id, "Coffee", null),
            new ToDoItemEntity(chores.Id, "Touch grass", "In progress")); // use a value from your domain status list

        await ctx.SaveChangesAsync(ct);
    }

    public static void Seed(DbContext ctx) =>
        SeedAsync(ctx, CancellationToken.None).GetAwaiter().GetResult();
}
