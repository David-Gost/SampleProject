using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Dommel;
using ElmahCore;
using ElmahCore.Mvc;
using Hangfire;
using Hangfire.MySql;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using Oracle.ManagedDataAccess.Client;
using SampleProject.Base.Interface.DB.Repositories;
using SampleProject.Base.Repositories;
using SampleProject.Base.Util.DB;
using SampleProject.Base.Util.DB.Dapper.DommelBuilder;
using SampleProject.Base.Util.Filter;
using SampleProject.Helpers;
using SampleProject.Interface.Elmah;
using SampleProject.Jobs;
using SampleProject.Middleware;
using SampleProject.Services.DB.User;
using SQLite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//於此撰寫手動注入
builder.InitDbContext();

//使用 AutoFuc注入符合命名空間的class
var config = builder.Configuration;
var env = builder.Environment;

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        var fileTypeRegex = new Regex(@".(Services|Repositories)");
        var assembly = Assembly.GetExecutingAssembly();
        containerBuilder.RegisterAssemblyTypes(assembly)
            .Where(fileType => fileType.Namespace != null &&
                               !fileType.Namespace.Contains(".Base.") &&
                               fileTypeRegex.IsMatch(fileType.Namespace))
            .AsSelf()
            .InstancePerLifetimeScope();
    });

// 取得專案目錄路徑
var projectDirectory = Directory.GetCurrentDirectory();

//系統設定
var systemOptionDictionary = builder.Configuration.GetSection("SystemOption").GetChildren().ToDictionary(x => x.Key);

#region Hangfire設定

var hangfireConfig = systemOptionDictionary.GetValueOrDefault("HangfireConfig");
var hangfireStatus = false;
if (hangfireConfig != null)
{
    hangfireStatus = hangfireConfig.GetValue("IsOn", false);

    if (hangfireStatus)
    {
        builder.Services.AddHangfire(configuration =>
        {
            configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            var hangfireStorageType = hangfireConfig.GetValue<string>("Driver") ?? "";
            var hangfireStorageDictionary = hangfireConfig.GetSection("StorageInfo");
            var hangfireStorageInfo = hangfireStorageDictionary.GetSection(hangfireStorageType);

            var dbConnectStr = "";
            switch (hangfireStorageType)
            {
                default:
                    
                    //有例外類型，強制不啟用
                    hangfireStatus = false;
                    break;

                case "Sqlite":

                    var dbName = hangfireStorageInfo.GetValue<string>("DbName") ?? "hangfire.db";
                    var dbPath = hangfireStorageInfo.GetValue<string>("DbPath");

                    if (!string.IsNullOrEmpty(dbPath))
                    {
                        // 連結 hangfire 資料夾路徑
                        var hangfireDirectory = Path.Combine(projectDirectory, dbPath);

                        // 檢查hangfire資料夾是否存在，如果不存在則建立
                        if (!Directory.Exists(hangfireDirectory))
                        {
                            Directory.CreateDirectory(hangfireDirectory);
                        }

                        dbConnectStr = Path.Combine(env.ContentRootPath, "Hangfire", "hangfire.db");
                    }
                    else
                    {
                        dbConnectStr = dbName;
                    }

                    configuration.UseSQLiteStorage(dbConnectStr);
                    break;

                case "Mysql":

                    dbConnectStr = hangfireStorageInfo.GetValue<string>("Connection", "");
                    configuration.UseStorage(new MySqlStorage(
                        dbConnectStr,
                        new MySqlStorageOptions { TablesPrefix = "Hangfire" }));
                    break;
            }
        });
    }
}

if (hangfireStatus)
{
    builder.Services.AddHangfireServer();
    
    #region 注入排程工作

    // builder.AddExampleJob();
    // builder.AddSendMailJob();

    #endregion
}
#endregion

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    //Swagger相關設定

    // 讀取 XML 檔案產生 API 說明
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    //使用本身的model驗證
    options.SuppressModelStateInvalidFilter = true;
});

// Add services to the container.
builder.Services.AddRazorPages();

#region Elmah

//Elmah設定
var elmahConfig = systemOptionDictionary.GetValueOrDefault("ElmahConfig");
var elmahIsOn = elmahConfig.GetValue<bool>("IsOn", false);
var elmahUrlStatus = elmahConfig.GetValue<bool>("UrlPathOn", false);

if (elmahIsOn)
{
    builder.Services.AddElmah<XmlFileErrorLog>(options =>
    {
        options.LogPath = "~/Logs";

        if (elmahUrlStatus)
        {
            options.Path = "logs";
        }

        options.Notifiers.Add(new NotificationFilter());
        options.Filters.Add(new CmsErrorLogFilter());
    });
}

#endregion

builder.Services.AddControllers(options =>
{
    //加入自訂的model檢查
    options.Filters.Add<ModelValidationAttribute>();
});

#region 專案資源資料夾

//資料夾初始化
var pathDatas = builder.Configuration.GetSection("FilePath").GetChildren();
if (pathDatas.Any())
{
    var basePath = builder.Configuration.GetValue<string>("FilePath:Base");

    //檢查base資料夾是否存在
    FileHelper.CheckPath(basePath!);
    foreach (var configurationSection in pathDatas)
    {
        var pathType = configurationSection.Key;

        if (!pathType.Equals("Base"))
        {
            var childPathDatas = configurationSection.GetChildren();

            //依照設定建立目錄
            foreach (var pathData in childPathDatas)
            {
                var pathInfo = pathData.GetChildren().ToDictionary(x => x.Key);
                var pathName = pathInfo["PathName"].Value;

                if (!string.IsNullOrEmpty(pathName))
                {
                    FileHelper.CheckPath($"{basePath}/{pathName}");
                }
            }
        }
    }
}

#endregion

var app = builder.Build();

#region UrlPath驗證設定

var middlewareAuthConfig = systemOptionDictionary.GetValueOrDefault("MiddlewareAuthConfig");
if (middlewareAuthConfig != null)
{
    var middlewareAuthStatus = middlewareAuthConfig.GetValue<bool>("IsOn", false);

    //取的需要登入的路由清單
    var urlPathList = middlewareAuthConfig.GetSection("UrlPaths").Get<IEnumerable<string>>() ?? [];

    //啟用驗證檢查
    if (middlewareAuthStatus)
    {
        app.UseWhen(context =>
                urlPathList.Any(x =>
                    context.Request.Path.ToString().Split("/", StringSplitOptions.RemoveEmptyEntries).First()
                        .Contains(x, StringComparison.CurrentCultureIgnoreCase)),
            appBuilder => { appBuilder.UseMiddleware<UrlPathAuthMiddleware>(); });
    }
}

#endregion

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

//加入額外Builder
DommelMapper.AddSqlBuilder(typeof(OracleConnection), new OracleSqlBuilder());

//於開發模式時無視Cors問題
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowSpecificOrigin");
}

if (elmahIsOn)
{
    app.UseElmah();
}

app.UseStaticFiles();

#region Hangfire

if (hangfireStatus)
{
    //Hangfire相關啟用設定
    app.UseHangfireDashboard("/hangfire");

    #region 加入排程
    
    //以下加入定義的排程
    // app.SetExampleJob();
    // app.SetSendMailJob();
    #endregion
}

#endregion

app.UseMiddleware<UrlPathAuthMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandleMiddleware>();
// app.UseAuthentication();
// app.UseAuthorization();
app.MapControllers();
app.Run();