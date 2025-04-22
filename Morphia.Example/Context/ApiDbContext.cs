using Microsoft.EntityFrameworkCore;
using Morphia.Example.Models;

namespace Morphia.Example.Context;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity => {
            entity.HasKey(c => c.ID);
            entity.Property(c => c.Name).HasMaxLength(250).IsRequired();
        });

        modelBuilder.Entity<Employe>(entity => {
            entity.HasKey(e => e.ID);
            entity.HasOne(e => e.Company).WithMany(c => c.Employees).HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.ID).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(250).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}