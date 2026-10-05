using Microsoft.EntityFrameworkCore;
using ToDo.Domain;

namespace ToDo.Persistence;

public class ToDoDbContext : DbContext
{
    public ToDoDbContext(DbContextOptions<ToDoDbContext> options) : base(options)
    { 
    }

    public DbSet<ToDoListEntity> ToDoLists { get; set; }
    public DbSet<ToDoItemEntity> ToDoItems { get; set; }

    //public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    //{
    //    return base.SaveChangesAsync(cancellationToken);
    //}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ToDoDbContext).Assembly);
    }
}
