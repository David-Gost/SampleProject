using System.Data;
using SampleProject.Base.Interface.DB.Repositories;
using SampleProject.Base.Repositories;
using SampleProject.Base.Util.DB.Dapper;
using SampleProject.Base.Util.DB.EFCore;
using SampleProject.Database;

namespace SampleProject.Base.Util.DB;

public static class DbContextExtensions
{
    /// <summary>
    /// 注入db相關
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder InitDbContext(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IBaseDbConnection, BaseDbConnection>();
        builder.Services.AddScoped<DapperContextManager>();
        builder.Services.AddScoped<DbContextManager>();

        //注入Dapper IDbConnection
        builder.Services.AddScoped<IDbConnection>((serviceProvider) =>
        {
            var dbContextManager = serviceProvider.GetRequiredService<DapperContextManager>();
            return dbContextManager.CreateDbConnection();
        });

        //注入EF Core DbContext
        builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var dbContextManager = serviceProvider.GetRequiredService<DbContextManager>();
            dbContextManager.ConfigureDbContext(options);
        });

        return builder;
    }
}