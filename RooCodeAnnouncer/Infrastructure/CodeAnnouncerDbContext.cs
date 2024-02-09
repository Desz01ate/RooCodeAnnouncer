using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RooCodeAnnouncer.Infrastructure.Entities;

namespace RooCodeAnnouncer.Infrastructure;

public class CodeAnnouncerDbContext : DbContext, IDesignTimeDbContextFactory<CodeAnnouncerDbContext>
{
    public CodeAnnouncerDbContext()
    {
    }

    public CodeAnnouncerDbContext(DbContextOptions<CodeAnnouncerDbContext> options)
        : base(options)
    {
    }

    public DbSet<PublishedItemCode> PublishedItemCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PublishedItemCode>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Rewards)
                .IsRequired();
        });
    }

    public CodeAnnouncerDbContext CreateDbContext(string[] args)
    {
        const string connectionString = "Data Source=local.db;";

        var optionsBuilder = new DbContextOptionsBuilder<CodeAnnouncerDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new CodeAnnouncerDbContext(optionsBuilder.Options);
    }
}