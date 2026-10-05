using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToDo.Domain;

namespace ToDo.Persistence.Configs;

public class ToDoListConfig : IEntityTypeConfiguration<ToDoListEntity>
{
    public void Configure(EntityTypeBuilder<ToDoListEntity> builder)
    {
        // Configure keys
        builder.HasKey(x => x.Id); // Primary key

        builder.HasIndex(x => x.ListTitle).IsUnique(); // Unique index on ListTitle
        builder.Property(x => x.ListTitle).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ListDescription).HasMaxLength(500);
        builder.Property(x => x.CreatedOn).IsRequired();

        // Configure relationships
        builder.HasMany(x => x.ToDoItems)
               .WithOne(x => x.ToDoList)
               .HasForeignKey(x => x.ToDoListId)
               .OnDelete(DeleteBehavior.Cascade);

        // Configure concurrency token
        builder.Property(x => x.ModifiedOn).IsConcurrencyToken(true).IsRequired();
    }
}
