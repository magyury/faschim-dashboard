using Microsoft.EntityFrameworkCore;
using FaschimPivotApp.Backend.Models;
using Microsoft.Extensions.Configuration;

namespace FaschimPivotApp.Backend.Data;

public class MyDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public MyDbContext(DbContextOptions<MyDbContext> options, IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<FullKeplero> FullKeplero { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        var tableName = _configuration["Database:TableName"] ?? "FullKeplero";
        
        modelBuilder.Entity<FullKeplero>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });
    }
}
