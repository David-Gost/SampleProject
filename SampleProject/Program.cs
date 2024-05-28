using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Dommel;
using ElmahCore;
using ElmahCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using SampleProject.Base.Interface.DB.Repositories;
using SampleProject.Base.Util;
using SampleProject.Base.Util.DB.DommelBuilder;
using SampleProject.Base.Util.Filter;
using SampleProject.Helpers;
using SampleProject.Interface.Elmah;
using SampleProject.Middleware;
using SampleProject.Services.DB.User;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//於此撰寫手動注入
builder.Services.AddScoped<IBaseDbConnection, BaseDbConnection>();

//使用 AutoFuc注入符合命名空間的class
var config = builder.Configuration;
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

//Elmah設定
var elmahStatus = config.GetValue<bool>("SystemOption:Elmah:status");
var elmahUrlStatus = config.GetValue<bool>("SystemOption:Elmah:urlStatus");

if (elmahStatus)
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

builder.Services.AddControllers(options =>
{
    //加入自訂的model檢查
    options.Filters.Add<ModelValidationAttribute>();
});

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

var app = builder.Build();

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

if (elmahStatus)
{
    app.UseElmah();  
}

app.UseStaticFiles();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandleMiddleware>();
// app.UseAuthentication();
// app.UseAuthorization();
app.MapControllers();
app.Run();