using System.Reflection;
using Base.Helpers;
using Microsoft.EntityFrameworkCore;
using Base.Interface.DB;

namespace Base.Util.DB.EFCore;

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
            entity.deletedAt = nowTime;
            entityEntry.State = EntityState.Modified;
            
            // 遍歷所有屬性，將除 deletedAt 外的所有屬性標記為未修改
            foreach (var property in entityEntry.Properties)
            {
                if (property.Metadata.Name != nameof(ISoftDeletable.deletedAt))
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
                    
                    entity.createdAt = nowTime;
                    break;
                case EntityState.Modified:
                    
                    // 保持 createdAt 為原始值
                    if (entityEntry.Property(nameof(ITimestampable.createdAt)).IsModified)
                    {
                        entityEntry.Property(nameof(ITimestampable.createdAt)).CurrentValue = 
                            entityEntry.Property(nameof(ITimestampable.createdAt)).OriginalValue;
                    }
                    entityEntry.Property(nameof(ITimestampable.createdAt)).IsModified = false;

                    // 處理軟刪除
                    if (entity is ISoftDeletable)
                    {
                        entityEntry.Property(nameof(ISoftDeletable.deletedAt)).IsModified = false;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            entity.updatedAt = nowTime;
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
    /// 存在deletedAt欄位時，預設不撈取有
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <typeparam name="T"></typeparam>
    private void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : class, ISoftDeletable
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.deletedAt == null);
    }
}