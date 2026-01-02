using System.Dynamic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using SampleProject.Database;
using Dommel;
using Hangfire;
using Hangfire.MySql;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using Oracle.ManagedDataAccess.Client;
using Base.Util.DB;
using Base.Util.DB.Dapper.DommelBuilder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SampleProject.Base.Util.DB;
using SampleProject.Helpers;
using SampleProject.Jobs;
using SampleProject.Middleware;
using SampleProject.Middleware.Auth.Base;
using SampleProject.Middleware.Hangfire;
using SampleProject.Services.DB.User;
using SampleProject.Util.Filter;
using SQLite;
using Formatting = Newtonsoft.Json.Formatting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//於此撰寫手動注入
builder.InitDbContext();

//使用 AutoFuc注入符合命名空間的class
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
                        
                        // Prevent path traversal
                        var fullHangfirePath = Path.GetFullPath(hangfireDirectory);
                        var fullProjectPath = Path.GetFullPath(projectDirectory);

                        if (!fullHangfirePath.StartsWith(fullProjectPath, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException("Invalid path specified for Hangfire database. Path traversal is not allowed.");
                        }

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

    options.AddSecurityRequirement(doc =>
    {
        doc.Security ??= new List<OpenApiSecurityRequirement>();
        var requirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", doc),
                []
            }
        };
        doc.Security.Add(requirement);
        return requirement;
    });
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    //使用本身的model驗證
    options.SuppressModelStateInvalidFilter = true;
});

// Add services to the container.
builder.Services.AddRazorPages();

#region 系統logger

var requestLogConfig = systemOptionDictionary.GetValueOrDefault("RequestLog");
var requestLogStatus = requestLogConfig?.GetValue("IsOn", false) ?? false;
var requestLogPath = requestLogConfig?.GetValue<string>("LogPath", "logs");
var requestLogViewPath = requestLogConfig?.GetValue<string>("LogViewPath", "views");

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

#region 健康度檢查

var healthCheckConfig = systemOptionDictionary.GetValueOrDefault("HealthCheckConfig");
var healthCheckIsOn = healthCheckConfig!.GetValue("IsOn", false);

if (healthCheckIsOn)
{
    builder.Services.AddHealthChecks();
}

#endregion

#region Api Auth規則設定

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddScheme<BaseSchemeOptions, BaseTokenHandler>(JwtBearerDefaults.AuthenticationScheme, options => { });

#endregion

#region Session 服務

var sessionConfig = systemOptionDictionary.GetValueOrDefault("SessionConfig");
var sessionIsOn = sessionConfig!.GetValue("IsOn", false);

if (sessionIsOn)
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        //儲存30分鐘
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });
}

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

#region 設定轉發標頭

// 設定轉發標頭選項
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    
    // 清除已知的代理和網路，以允許來自任何 IP 的代理轉發
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();
});

#endregion

var app = builder.Build();

app.UseForwardedHeaders();

#region Session 服務

if (sessionIsOn)
{
    app.UseSession();
}

#endregion

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

#region request Log

app.UseMiddleware<ExceptionHandleMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();


if (requestLogStatus)
{
    #region request Log 畫面相關

    var vueAppPath = Path.Combine(builder.Environment.WebRootPath, requestLogPath, "views");
    var vueFileProvider = new PhysicalFileProvider(vueAppPath);

    // 為任何以 /logs 開頭的請求建立一個專屬的中介軟體分支
    app.MapWhen(context => context.Request.Path.StartsWithSegments($"/{requestLogPath}"), logsApp =>
    {
        logsApp.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = vueFileProvider,
            // 告訴中介軟體，請求路徑中的 "/logs" 部分
            // 對應到 vueFileProvider 的根目錄。
            // 例如：請求 /logs/assets/app.js -> 在 vueFileProvider 根目錄下尋找 /assets/app.js
            RequestPath = $"/{requestLogPath}"
        });

        // 後備機制：如果請求的不是一個已知的靜態檔案，則回傳 index.html
        // 這必須在 UseStaticFiles 之後
        logsApp.Run(async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(Path.Combine(vueAppPath, "index.html"));
        });
    });

    #endregion

    #region request Log API

    var logsApi = app.MapGroup("/LogsApi");
    logsApi.WithMetadata(new ApiExplorerSettingsAttribute { IgnoreApi = true });

    //使用mini API處理request log   
    var logXmlPath = Path.Combine("wwwroot", requestLogPath);
    dynamic logResponse = new ExpandoObject();
    logResponse.status = "success";
    logResponse.data = null;
    logResponse.message = "";

    // 取得 log 清單
    logsApi.MapPost("/GetLogList", ([FromBody] Dictionary<string, object?>? requestBody = null) =>
    {
        if (requestBody is null or { Count: 0 })
        {
            requestBody = new Dictionary<string, object?>
            {
                { "keyword", "" },
                { "statusCode", "" },
                { "startTime", null },
                { "endTime", null },
                { "pageNumber", "1" },
                { "pageSize", "10" }
            };
        }

        // 關鍵字
        var keyword = "";
        if (requestBody.TryGetValue("keyword", out var keywordVal))
        {
            if (keywordVal != null) keyword = keywordVal.ToString();
        }

        // httpStatusCode
        var statusCode = 0;
        if (requestBody.TryGetValue("statusCode", out var statusCodeVal))
        {
            if (statusCodeVal != null && int.TryParse(statusCodeVal.ToString(), out var statusCodeCache))
            {
                statusCode = statusCodeCache;
            }
        }

        // 起始時間
        DateTime? startTime = null;
        if (requestBody.TryGetValue("startTime", out var startDateVal))
        {
            if (startDateVal != null && DateTime.TryParse(startDateVal.ToString(), out var parsedTime))
            {
                startTime = parsedTime;
            }
        }

        // 結束時間
        DateTime? endTime = null;
        if (requestBody.TryGetValue("endTime", out var endDateVal))
        {
            if (endDateVal != null && DateTime.TryParse(endDateVal.ToString(), out var parsedTime))
            {
                // 不為空值時，變為日期的最後一分鍾
                endTime = parsedTime;
            }
        }

        // 頁碼
        var pageNumber = 0;
        if (requestBody.TryGetValue("pageNumber", out var pageNumberVal))
        {
            if (pageNumberVal != null) pageNumber = int.Parse(pageNumberVal.ToString()!);
        }

        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        // 每頁資料筆數
        var pageSize = 0;
        if (requestBody.TryGetValue("pageSize", out var pageSizeVal))
        {
            if (pageSizeVal != null) pageSize = int.Parse(pageSizeVal.ToString()!);
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        var projectRoot = Path.GetFullPath(projectDirectory);
        var fullXmlPath = Path.GetFullPath(logXmlPath);

        if (!fullXmlPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid path specified. Path traversal is not allowed.");
        }

        var allFileDatas = Directory.GetFiles(logXmlPath, "*.xml")
            .Select(f =>
            {
                try
                {
                    var xmlContent = File.ReadAllText(f);
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlContent);
                    var errorNode = xmlDoc.SelectSingleNode("/error");
                    var fileName = Path.GetFileName(f);
                    var lastModifiedDate = File.GetLastWriteTime(f).ToString("yyyy-MM-dd HH:mm:ss");

                    //抓serverVariables中的Method還有Path
                    var method = errorNode?.SelectSingleNode("serverVariables/item[@name='Method']/value")
                        ?.Attributes?["string"]?.Value ?? "";
                    var path = errorNode?.SelectSingleNode("serverVariables/item[@name='Path']/value")
                        ?.Attributes?["string"]?.Value ?? "";

                    //抓狀態碼
                    if (errorNode?.Attributes?["statusCode"] != null)
                    {
                        var statusCode = int.Parse(errorNode.Attributes["statusCode"]!.Value);

                        return new
                        {
                            statusCode,
                            path,
                            method,
                            fileName,
                            fileDate = lastModifiedDate,
                        };
                    }
                }
                catch (Exception e)
                {
                    // ignored
                }

                return null;
            })
            .Where(f => f != null).OrderByDescending(f => f.fileDate).ToList();

        var filteredDatas = allFileDatas;

        // 關鍵字搜尋
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            filteredDatas = allFileDatas.Where(f =>
                f != null &&
                (f.fileName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                 f.path.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                 f.method.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                 f.statusCode.ToString().Contains(keyword))
            ).ToList();
        }

        // 狀態碼搜尋
        if (statusCode > 0)
        {
            filteredDatas = filteredDatas.Where(f => f != null && f.statusCode == statusCode).ToList();
        }

        // 起始日搜尋
        if (startTime.HasValue)
        {
            filteredDatas = filteredDatas.Where(f => f != null && DateTime.Parse(f.fileDate) >= startTime).ToList();
        }

        // 結束日搜尋
        if (endTime.HasValue)
        {
            filteredDatas = filteredDatas.Where(f => f != null && DateTime.Parse(f.fileDate) <= endTime).ToList();
        }

        var dataCount = filteredDatas.Count;
        var totalPages = (int)Math.Ceiling((double)dataCount / pageSize);

        //頁碼判斷，大於資料頁數時，變為當下最大頁數
        if (totalPages > 0 && pageNumber > totalPages)
        {
            pageNumber = totalPages;
        }

        var pagedData = filteredDatas
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var logListResponse = new
        {
            dataCount,
            totalPages,
            pageNumber,
            pageSize,
            data = pagedData,
        };

        return Results.Json(logListResponse) as IResult;
    });

    // 讀取單一 XML 檔案
    logsApi.MapGet("/GetLogDetail/{file}", (string file) =>
    {
        var logDirectory = Path.Combine("wwwroot", requestLogPath);
        var fileName = Path.GetFileName(file);
        var path = Path.Combine(logDirectory, fileName);

        // 將路徑正規化以防止目錄遍歷攻擊
        var fullLogDirectory = Path.GetFullPath(logDirectory);
        var fullFilePath = Path.GetFullPath(path);

        if (!fullFilePath.StartsWith(fullLogDirectory))
        {
            logResponse.status = "error";
            logResponse.message = $"File not found: {file}";
            logResponse.data = null;

            return Results.BadRequest(logResponse);
        }

        var xmlContent = File.ReadAllText(path);

        // 將 XML 轉換為 JSON
        var xmlDoc = new System.Xml.XmlDocument();
        xmlDoc.LoadXml(xmlContent);
        var jsonText = JsonConvert.SerializeXmlNode(xmlDoc);
        var jsonObject = JsonConvert.DeserializeObject<JObject>(jsonText);

        // 重組 JSON 結構
        if (jsonObject?["error"] is JObject errorNode)
        {
            var restructuredError = new Dictionary<string, object>();
            var exceptionDic = new Dictionary<string, object>();

            // 處理 error 節點下的屬性和 serverVariables
            foreach (var property in errorNode.Properties())
            {
                switch (property.Name)
                {
                    case "serverVariables":

                        var items = property.Value["item"];
                        var serverVariableDict = new Dictionary<string, object>();

                        if (items is JArray itemsArray)
                        {
                            foreach (var item in itemsArray)
                            {
                                var name = item["@name"]?.ToString();
                                var value = item["value"]?["@string"]?.ToString().Replace("\\\"", "").Replace("\"", "");

                                if (string.IsNullOrEmpty(name)) continue;
                                object showVal = value switch
                                {
                                    "True" => true,
                                    "False" => false,
                                    _ => value ?? "".Replace("\\\"", "").Replace("\"", "")
                                };
                                serverVariableDict.Add(name, showVal);
                            }
                        }

                        restructuredError[property.Name] = serverVariableDict;

                        break;

                    case "messageLog":

                        var messageList = new List<Dictionary<string, string>>();
                        var messageNode = property.Value["message"];

                        switch (messageNode)
                        {
                            case JArray messageArray:
                                // 多個 message 節點
                                messageList.AddRange(messageArray.Select(msg => msg.Children<JProperty>()
                                    .ToDictionary(p => p.Name.TrimStart('@'), p => p.Value.ToString())));
                                break;
                            case JObject messageObject:
                            {
                                // 單一 message 節點
                                var msgDict = messageObject.Properties()
                                    .ToDictionary(p => p.Name.TrimStart('@'), p => p.Value.ToString());
                                messageList.Add(msgDict);
                                break;
                            }
                        }

                        restructuredError[property.Name] = messageList;
                        break;

                    case "queryString":

                        var queryItems = property.Value["item"];
                        var queryDict = new Dictionary<string, object>();

                        switch (queryItems)
                        {
                            case JArray queryItemsArray:
                            {
                                foreach (var item in queryItemsArray)
                                {
                                    var name = item["@name"]?.ToString();
                                    var value = item["value"]?["@string"]?.ToString();
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        queryDict.Add(name, value ?? "");
                                    }
                                }

                                break;
                            }
                            case JObject queryItemObject:
                            {
                                var name = queryItemObject["@name"]?.ToString();
                                var value = queryItemObject["value"]?["@string"]?.ToString();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    queryDict.Add(name, value ?? "");
                                }

                                break;
                            }
                        }

                        restructuredError[property.Name] = queryDict;

                        break;

                    case "requestBody":

                        var requestBodyContent = property.Value["#cdata-section"]?.ToString();
                        if (!string.IsNullOrEmpty(requestBodyContent))
                        {
                            object? parsedBody;
                            try
                            {
                                // 嘗試解析為 JSON 物件
                                if (requestBodyContent.Trim().StartsWith($"{{"))
                                {
                                    var cacheData =
                                        JsonConvert.DeserializeObject<Dictionary<string, JToken>>(requestBodyContent);

                                    object? Normalize(JToken token)
                                    {
                                        return token switch
                                        {
                                            JValue v => v.Value,
                                            JArray a => a.Select(Normalize).ToList(),
                                            JObject o => o.Properties()
                                                .ToDictionary(p => p.Name, p => Normalize(p.Value)),
                                            _ => null
                                        };
                                    }


                                    parsedBody = cacheData!.ToDictionary(x => x.Key, x => Normalize(x.Value));
                                }
                                // 嘗試解析為 Form Data
                                else
                                {
                                    parsedBody = requestBodyContent.Split('&')
                                        .Select(part => part.Split('='))
                                        .Where(pair => pair.Length == 2)
                                        .ToDictionary(
                                            pair => Uri.UnescapeDataString(pair[0]),
                                            object (pair) => Uri.UnescapeDataString(pair[1])
                                        );
                                }
                            }
                            catch
                            {
                                // 如果解析失敗，則直接返回原始字串
                                parsedBody = requestBodyContent;
                            }

                            restructuredError["request"] = parsedBody ?? "";
                        }
                        else
                        {
                            restructuredError["request"] = string.Empty;
                        }

                        break;

                    case "identityUser":

                        if (property.Value is JObject identityUserNode)
                        {
                            var authValue = identityUserNode["auth"]?.ToString();
                            var userNameValue = identityUserNode["userName"]?.ToString();

                            var identityInfo = new Dictionary<string, object?>
                            {
                                { "auth", authValue?.ToLower() == "true" },
                                { "userName", userNameValue }
                            };
                            restructuredError[property.Name] = identityInfo;
                        }

                        break;

                    default:

                        var exceptionDicKey = property.Name[1..];
                        var exceptionDicVal = property.Value.ToString();

                        if (exceptionDicKey.Equals("detail"))
                        {
                            exceptionDicVal = exceptionDicVal.Replace(Environment.NewLine, "<br/>");
                        }

                        exceptionDic.Add(exceptionDicKey, exceptionDicVal);
                        break;
                }
            }

            restructuredError.Add("exception", exceptionDic);

            logResponse.status = "success";
            logResponse.message = "";
            logResponse.data = restructuredError;
        }
        else
        {
            // 如果沒有 error 節點，直接回傳原始轉換的 JSON
            logResponse.status = "success";
            logResponse.message = "XML content does not contain 'error' node.";
            logResponse.data = jsonObject;
        }

        return Results.Json(logResponse);
    });

    #endregion

    //導向連結，會依照設定檔中路徑導向畫面
    app.MapGet($"/{requestLogPath}", async context =>
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(Path.Combine("wwwroot", requestLogPath, requestLogViewPath, "index.html"));
    });
}

#endregion

app.UseStaticFiles();

#region Hangfire

if (hangfireStatus)
{
    //Hangfire相關啟用設定
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireAuthorizationFilter()]
    });

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

#region 健康度檢查

if (healthCheckIsOn)
{
    var healthCheckPath = healthCheckConfig!.GetValue<string>("Path") ?? "";
    if (string.IsNullOrEmpty(healthCheckPath) || healthCheckPath.TrimStart('/') == "/")
    {
        healthCheckPath = "/health";
    }

    app.MapHealthChecks($"/{healthCheckPath.TrimStart('/')}", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var json = new
            {
                status = report.Status.ToString(),
                errors = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    data = entry.Value.Data
                })
            };

            await context.Response.WriteAsync(JsonConvert.SerializeObject(json, Formatting.Indented));
        }
    });
}

#endregion

app.UseMiddleware<UrlPathAuthMiddleware>();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();