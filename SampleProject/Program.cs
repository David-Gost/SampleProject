using System.Reflection;
using Dommel;
using Oracle.ManagedDataAccess.Client;
using SampleProject.Base.Util.DB.DommelBuilder;
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
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

//加入額外Builder
DommelMapper.AddSqlBuilder(typeof(OracleConnection), new OracleSqlBuilder());

app.UseHttpsRedirection();

//於開發模式時無視Cors問題
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowSpecificOrigin");
}

app.UseStaticFiles();
app.MapControllers();
app.Run();