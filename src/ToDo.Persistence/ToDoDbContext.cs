using Microsoft.EntityFrameworkCore;

namespace ToDo.Persistence;

public class ToDoDbContext : DbContext
{
    public ToDoDbContext(DbContextOptions options) : base(options)
    { 
    }

    //public DbSet<>  { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ToDoDbContext).Assembly);
    }
}
