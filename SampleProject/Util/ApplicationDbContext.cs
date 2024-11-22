using Microsoft.EntityFrameworkCore;
using SampleProject.Base.Util.DB.EFCore;
using SampleProject.Models.DB.Common;

namespace SampleProject.Util;

public class ApplicationDbContext : BaseDbContext
{
    public DbSet<TempMailModel> TempMails { get; set; }
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // 在這裡進行其他模型配置
        modelBuilder.Entity<TempMailModel>();
    }
}