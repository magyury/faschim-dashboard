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

    // First table
    public DbSet<FullKeplero> FullKeplero { get; set; }
    
    // Second table
    public DbSet<SecondTable> SecondTable { get; set; }
    
    // Keplero Compare table
    public DbSet<KepleroCompare> KepleroCompare { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure first table
        var tableName = _configuration["Database:TableName"] ?? "FullKeplero_Backup";
        modelBuilder.Entity<FullKeplero>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        
        // Configure keplero_compare_backup table
        modelBuilder.Entity<KepleroCompare>(entity =>
        {
            entity.ToTable("keplero_compare_backup");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            // Indexes for better performance
            entity.HasIndex(e => e.ItemId);
            entity.HasIndex(e => e.Protocollo);
            entity.HasIndex(e => e.DataEsito);
        });
    }
}
