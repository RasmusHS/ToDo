using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToDo.Domain;

namespace ToDo.Persistence.Configs;

public class ToDoItemConfig : IEntityTypeConfiguration<ToDoItemEntity>
{
    public void Configure(EntityTypeBuilder<ToDoItemEntity> builder)
    {
        // Configure keys
        builder.HasKey(x => x.Id); // Primary key

        builder.Property(x => x.Text).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Status).HasMaxLength(100);
        builder.Property(x => x.CreatedOn).IsRequired();

        // Configure relationships
        builder.HasOne(x => x.ToDoList)
               .WithMany(x => x.ToDoItems)
               .HasForeignKey(x => x.ToDoListId)
               .OnDelete(DeleteBehavior.Cascade);

        // Configure concurrency token
        builder.Property(x => x.ModifiedOn).IsConcurrencyToken(true).IsRequired();
    }
}
