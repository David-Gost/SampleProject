using Microsoft.EntityFrameworkCore;
using Base.Util.DB.EFCore;
using SampleProject.Models.DB.Common;
using SampleProject.Models.DB.User;

namespace SampleProject.Database;

public class ApplicationDbContext : BaseDbContext
{
    public DbSet<TempMailModel> TempMails { get; set; }
    public DbSet<AuthUsers?> UserAuths { get; set; }
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // 在這裡進行其他模型配置
        modelBuilder.Entity<TempMailModel>();
        modelBuilder.Entity<AuthUsers>();
        
    }
}