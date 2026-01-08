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
    public DbSet<KepleroCompareEntity> KepleroCompare { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure FullKeplero table
        var tableName = _configuration["Database:TableName"] ?? "FullKeplero";
        modelBuilder.Entity<FullKeplero>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(e => e.NumeroProtocollo);
        });
        
        // Configure keplero_compare table
        var compareTableName = _configuration["Database:SecondTableName"] ?? "keplero_compare";
        modelBuilder.Entity<KepleroCompareEntity>(entity =>
        {
            entity.ToTable(compareTableName);
            entity.HasKey(e => e.Protocollo);
            
            // Indexes for better performance
            entity.HasIndex(e => e.ItemId);
            entity.HasIndex(e => e.Protocollo);
            entity.HasIndex(e => e.DataEsito);
        });
    }
}
