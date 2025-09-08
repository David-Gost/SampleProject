using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using SampleProject.Database;
using Microsoft.EntityFrameworkCore;

namespace SampleProject.Base.Util.DB.EFCore;

/// <summary>
/// 多型載入器
/// </summary>
public class PolymorphicLoader
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private string _prefix = "";

    // 可為每個 targetType 指定多層 Include 路徑
    private readonly Dictionary<string, string[]> _includePaths = new();

    public PolymorphicLoader(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void SetPrefix(string prefix)
    {
        _prefix = prefix;
    }
    
    /// <summary>
    /// 註冊 targetType 的 Include 路徑
    /// </summary>
    /// <param name="typeName">多型對應model</param>
    /// <param name="includePaths">include對象</param>
    public void RegisterIncludes(string typeName, params string[] includePaths)
    {
        _includePaths[typeName] = includePaths;
    }

    /// <summary>
    /// 載入多型的關聯資料
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="Exception"></exception>
    public async Task LoadAsync<T>(object? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (input == null) return;

        var isEnumerable = input is IEnumerable<T>;
        var models = isEnumerable ? ((IEnumerable<T>)input).ToList() : [(T)input];

        var typeProp = typeof(T).GetProperty($"{_prefix}Type");
        var idProp = typeof(T).GetProperty($"{_prefix}Id");
        var targetProp = typeof(T).GetProperty($"{_prefix}");

        if (typeProp == null || idProp == null || targetProp == null) return;

        var grouped = models
            .Where(m => typeProp.GetValue(m) is string && idProp.GetValue(m) is int)
            .GroupBy(m => typeProp.GetValue(m)!.ToString());

        foreach (var group in grouped)
        {
            var typeName = group.Key!;
            var ids = group.Select(x => (int)idProp.GetValue(x)!).Distinct().ToList();

            var targetType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);

            if (targetType == null) continue;

            var dbSet = _dbContext.GetType()
                .GetMethod("Set", Type.EmptyTypes)!
                .MakeGenericMethod(targetType)
                .Invoke(_dbContext, null) as IQueryable ?? throw new Exception("DbSet is null");

            // 動態 Include
            if (_includePaths.TryGetValue(typeName, out var paths))
            {
                var dynamicDbSet = paths.Aggregate<string?, dynamic>(dbSet, (current, path) => EntityFrameworkQueryableExtensions.Include(current, path)); // 轉成 dynamic
                dbSet = dynamicDbSet;
            }

            // 找主鍵
            var keyProp = targetType.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase) ||
                                     p.GetCustomAttributes(typeof(KeyAttribute), true).Length != 0);

            if (keyProp == null) continue;

            // 建立 Expression<Func<TargetType, bool>> 條件
            var parameter = Expression.Parameter(targetType, "e");
            var property = Expression.Property(parameter, keyProp.Name);

            var containsMethod = typeof(List<int>).GetMethod("Contains", [typeof(int)])!;
            var idsExpr = Expression.Constant(ids);
            var body = Expression.Call(idsExpr, containsMethod, property);
            var lambda = Expression.Lambda(body, parameter);

            var whereMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                .MakeGenericMethod(targetType);

            var query = (IQueryable)whereMethod.Invoke(null, [dbSet, lambda])!;

            var list = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync((dynamic)query, cancellationToken);

            // 回填
            foreach (var model in group)
            {
                var modelId = (int)idProp.GetValue(model)!;
                object? match = null;

                foreach (var item in list)
                {
                    var itemId = (int)keyProp.GetValue(item)!;
                    if (itemId == modelId)
                    {
                        match = item;
                        break;
                    }
                }

                if (match != null)
                {
                    targetProp.SetValue(model, match);
                }
            }
        }
    }
}