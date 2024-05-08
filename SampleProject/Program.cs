using System.Reflection;
using Dommel;
using ElmahCore;
using ElmahCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using SampleProject.Base.Util.DB.DommelBuilder;
using SampleProject.Helpers;
using SampleProject.Interface.Elmah;
using SampleProject.Services.DB.User;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//於此撰寫要注入的Service
builder.Services.AddScoped<UserService>();

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

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddElmah<XmlFileErrorLog>(options =>
{
    options.LogPath = "~/Logs";
    options.Path = "logs";
    // options.Notifiers.Add(new NotificationFilter());
    // options.Filters.Add(new CmsErrorLogFilter());
    options.LogRequestBody = true;
    
});

builder.Services.AddControllers();

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
                var pathInfo = pathData.GetChildren().ToDictionary(x=>x.Key);
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

app.UseElmah();
app.UseStaticFiles();
// app.UseAuthentication();
// app.UseAuthorization();
app.MapControllers();
app.Run();