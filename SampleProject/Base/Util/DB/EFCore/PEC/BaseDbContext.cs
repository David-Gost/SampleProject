using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SampleProject.Base.Interface.DB.PEC;
using SampleProject.Helpers;

namespace SampleProject.Base.Util.DB.EFCore.PEC;

/// <summary>
/// ef core的base，相關複寫都在這
/// </summary>
public class BaseDbContext : DbContext
{
    public BaseDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public override int SaveChanges()
    {
        UpdateTimestamps();
        HandleSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        HandleSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 軟刪除，於欄位中紀錄當下時間
    /// </summary>
    private void HandleSoftDelete()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is ISoftDeletable && e.State == EntityState.Deleted);

        var nowTime = SystemHelper.GetNowUtc();
        foreach (var entityEntry in entries)
        {
            var entity = (ISoftDeletable)entityEntry.Entity;
            entity.deletionDate = nowTime;
            entityEntry.State = EntityState.Modified;
            
            // 遍歷所有屬性，將除 deletedAt 外的所有屬性標記為未修改
            foreach (var property in entityEntry.Properties)
            {
                if (property.Metadata.Name != nameof(ISoftDeletable.deletionDate))
                {
                    property.IsModified = false;
                }
            }
        }
    }

    /// <summary>
    /// 新增、修改時紀錄時間
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is ITimestampable && e.State is EntityState.Added or EntityState.Modified);

        var nowTime = SystemHelper.GetNowUtc();;
        foreach (var entityEntry in entries)
        {
            var entity = (ITimestampable)entityEntry.Entity;

            switch (entityEntry.State)
            {
                case EntityState.Added:
                    
                    entity.creationDate = nowTime;
                    break;
                case EntityState.Modified:
                    
                    // 保持 createdAt 為原始值
                    if (entityEntry.Property(nameof(ITimestampable.creationDate)).IsModified)
                    {
                        entityEntry.Property(nameof(ITimestampable.creationDate)).CurrentValue = 
                            entityEntry.Property(nameof(ITimestampable.creationDate)).OriginalValue;
                    }
                    entityEntry.Property(nameof(ITimestampable.creationDate)).IsModified = false;

                    // 處理軟刪除
                    if (entity is ISoftDeletable)
                    {
                        entityEntry.Property(nameof(ISoftDeletable.deletionDate)).IsModified = false;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            entity.lastModifyDate = nowTime;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;
            var method = typeof(BaseDbContext).GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = method?.MakeGenericMethod(entityType.ClrType);
            genericMethod?.Invoke(this, [modelBuilder]);
        }
    }
    
    /// <summary>
    /// 存在deletedAt欄位時，預設不撈取有值資料
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <typeparam name="T"></typeparam>
    private void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : class, ISoftDeletable
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.deletionDate == null);
    }
}