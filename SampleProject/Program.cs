using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using SampleProject.Database;
using Dommel;
using ElmahCore;
using ElmahCore.Mvc;
using Hangfire;
using Hangfire.MySql;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.OpenApi.Models;
using Oracle.ManagedDataAccess.Client;
using Base.Util.DB;
using Base.Util.DB.Dapper.DommelBuilder;
using Base.Util.Filter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SampleProject.Base.Util.DB;
using SampleProject.Helpers;
using SampleProject.Interface.Elmah;
using SampleProject.Jobs;
using SampleProject.Middleware;
using SampleProject.Middleware.Auth.Base;
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

//注入seeder
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        var fileTypeRegex = new Regex(@".(Seeder)");
        var assembly = Assembly.GetExecutingAssembly();
        containerBuilder.RegisterAssemblyTypes(assembly)
            .Where(fileType => fileType.Namespace != null &&
                               fileType.Namespace.Contains(".Database.") &&
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

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT授權(數據將在請求header中進行傳輸)在下方輸入Bearer {token}即可，注意兩者之間有空格",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
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

#region 設定Cors策略

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials();
    });
});


builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAssertion(_ => true)
        .Build());

#endregion

builder.Services.AddControllers(options =>
{
    //加入自訂的model檢查
    options.Filters.Add<ModelValidationAttribute>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.MaxDepth = 64; // 增加最大深度
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保持屬性名稱不變
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull; // 忽略null值
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles; // 忽略循環引用
});

#region Api Auth規則設定

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddScheme<BaseSchemeOptions, BaseTokenHandler>(JwtBearerDefaults.AuthenticationScheme, options => { });
#endregion

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

#region 自動執行migration update

var autoMigrationConfig = systemOptionDictionary.GetValueOrDefault("AutoMigrationUpdateConfig");
var autoMigrationEnabled = autoMigrationConfig?.GetValue<bool>("IsOn") ?? false;

if (autoMigrationEnabled)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
            Console.WriteLine("Database migrations applied successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while migrating the database: {ex.Message}");
    }
}

#endregion

#region seeder

// 在應用啟動時執行 Seeder
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     var roleSeeder = services.GetRequiredService<RoleSeeder>();
//     await roleSeeder.SeedAsync();
// }

#endregion

app.UseMiddleware<UrlPathAuthMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandleMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();