using SampleProject.Database;
using Microsoft.EntityFrameworkCore;


namespace SampleProject.Base.Util.DB.EFCore;

public class DbContextManager
{
    private readonly IConfiguration _configuration;

    public DbContextManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 依照設定檔建立EF Core的連線
    /// </summary>
    /// <param name="dbConnectOption"></param>
    /// <returns></returns>
    public ApplicationDbContext CreateDbContext(string dbConnectOption = "Default" )
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        ConfigureDbContext(optionsBuilder,dbConnectOption);
        return new ApplicationDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// 設定DbContext，預設抓設定檔內DBConnection的Default
    /// </summary>
    /// <param name="optionsBuilder"></param>
    /// <param name="dbConnectOption"></param>
    /// <exception cref="ArgumentException"></exception>
    public void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder,string dbConnectOption = "Default"  )
    {
        var dbOption=_configuration.GetSection("DBConnection").GetSection(dbConnectOption);
        var dbConnectType = dbOption.GetValue<string>("DBType","")!.ToUpper();
        var dbConnectStr = dbOption.GetValue<string>("ConnectionString","")!;

        //依專案專案需求開啟對應
        switch (dbConnectType)
        {
            // case "MSSQL":
            //     optionsBuilder.UseSqlServer(dbConnectStr);
            //     break;
            
            case "MYSQL":
                optionsBuilder.UseMySql(dbConnectStr, ServerVersion.AutoDetect(dbConnectStr));
                break;
            case "ORACLE":
                optionsBuilder.UseOracle(dbConnectStr);
                break;
            case "POSTGRESQL":
                optionsBuilder.UseNpgsql(dbConnectStr);
                break;
            // 可以添加其他數據庫類型
            default:
                throw new ArgumentException("Unsupported database type", nameof(dbConnectType));
        }
    }
}