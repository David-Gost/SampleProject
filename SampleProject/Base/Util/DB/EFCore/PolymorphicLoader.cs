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
    /// 載入多型的關聯資料
    /// </summary>
    public async Task LoadAsync<T>(object? input, CancellationToken cancellationToken = default)
        where T : class
    {
        try
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

                var dbSet = _dbContext.GetType().GetMethod("Set", Type.EmptyTypes)!
                    .MakeGenericMethod(targetType)
                    .Invoke(_dbContext, null) as IQueryable;

                var keyProp = targetType.GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase) ||
                                         p.GetCustomAttributes(typeof(KeyAttribute), true).Length != 0);

                if (keyProp == null) continue;

                // 建立 Expression<Func<T, bool>> 查詢條件
                var parameter = Expression.Parameter(targetType, "e");
                var property = Expression.Property(parameter, keyProp.Name);

                var containsMethod = typeof(List<int>).GetMethod("Contains", [typeof(int)])!;
                var idsExpr = Expression.Constant(ids);
                var body = Expression.Call(idsExpr, containsMethod, property);

                var lambda = Expression.Lambda(body, parameter);

                var whereMethod = typeof(Queryable).GetMethods()
                    .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(targetType);

                var query = (IQueryable<object>)whereMethod.Invoke(null, [dbSet!, lambda])!;

                var list = await query.ToListAsync(cancellationToken);

                // 使用 Reflection 拿 mappingData 的 Id 欄位值
                foreach (var model in group)
                {
                    var modelId = (int)idProp.GetValue(model)!;
                    var match = list.FirstOrDefault(e => (int)keyProp.GetValue(e)! == modelId);
                    if (match != null)
                    {
                        targetProp.SetValue(model, match);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}