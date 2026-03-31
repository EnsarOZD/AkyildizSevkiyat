using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Polly;
using Polly.Extensions.Http;
using Akyildiz.Sevkiyat.Application;
using Akyildiz.Sevkiyat.Application.Common.Behaviors;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Infrastructure.Persistence;
using Akyildiz.Sevkiyat.WebApi.Middlewares;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Akyildiz.Sevkiyat.Infrastructure.Security;
using Akyildiz.Sevkiyat.Infrastructure.Email;
using Akyildiz.Sevkiyat.Application.Settings;
using Akyildiz.Sevkiyat.Infrastructure.ExternalServices.IssIp;
using Akyildiz.Sevkiyat.Infrastructure.ExternalServices.Netsis;
using Akyildiz.Sevkiyat.Infrastructure.Persistence.Seeding;
using Akyildiz.Sevkiyat.Infrastructure.BackgroundJobs;
using Akyildiz.Sevkiyat.Infrastructure.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ✅ JWT Claim mapping'ini temizle (sub -> nameidentifier dönüşümünü engeller)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// CORS Policy
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

if (allowedOrigins == null || allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("CORS AllowedOrigins configuration is missing or empty. Please check appsettings.json.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// FluentValidation (MediatR pipeline)
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Authentication dependencies
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ICurrentUserService, Akyildiz.Sevkiyat.WebApi.Services.CurrentUserService>();
builder.Services.AddHttpContextAccessor();

// Configure ISSIp Options
builder.Services.AddOptions<ISSIpOptions>()
    .Bind(builder.Configuration.GetSection("ISSIp"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IISSIpClient, Akyildiz.Sevkiyat.Infrastructure.ExternalServices.ISSIpClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<ISSIpOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
})
.AddPolicyHandler((sp, _) =>
{
    var retryLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("ISSIpClient.Retry");
    return HttpPolicyExtensions
        .HandleTransientHttpError() // HttpRequestException, 5xx, 408
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                retryLogger.LogWarning(
                    "ISS-IP isteği başarısız oldu. {RetryAttempt}. yeniden deneme {Delay}s sonra. Hata: {Reason}",
                    retryAttempt,
                    timespan.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            });
});

// Configure Shipment Settings
builder.Services.AddOptions<ShipmentSettings>()
    .Bind(builder.Configuration.GetSection("Shipment"));

// Configure Netsis Options
builder.Services.AddOptions<NetsisOptions>()
    .Bind(builder.Configuration.GetSection("Netsis"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<NetsisTokenCache>();

builder.Services.AddHttpClient<
    Akyildiz.Sevkiyat.Application.Interfaces.INetsisClient,
    NetsisClient>((sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<NetsisOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
});

// Configure Jwt Options
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Configure Smtp Options
builder.Services.AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection("Smtp"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Route Optimization Services
builder.Services.AddScoped<Akyildiz.Sevkiyat.Application.RouteOptimization.Interfaces.IIssSyncComparisonService, IssSyncComparisonService>();
builder.Services.AddScoped<Akyildiz.Sevkiyat.Application.RouteOptimization.Interfaces.IRouteOptimizationService, GoogleMapsRouteOptimizationService>();
builder.Services.AddHttpClient<GoogleMapsRouteOptimizationService>(client =>
{
    // Google Routes API v2 can reset HTTP/2 connections mid-response (ResponseEnded).
    // Force HTTP/1.1 for stable communication.
    client.DefaultRequestVersion = new Version(1, 1);
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
});

// Geocoding Service (adres → koordinat)
builder.Services.AddScoped<Akyildiz.Sevkiyat.Application.Common.Interfaces.IGeocodingService, GoogleGeocodingService>();
builder.Services.AddHttpClient<GoogleGeocodingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

// ISS-IP Import Orchestrator + Background Service
builder.Services.AddScoped<Akyildiz.Sevkiyat.Application.Common.Interfaces.IIssOrderImportOrchestrator, IssOrderImportOrchestrator>();
builder.Services.Configure<IssImportOptions>(builder.Configuration.GetSection("IssImport"));
builder.Services.AddHostedService<IssOrderImportBackgroundService>();

// Reconciliation enforcement
builder.Services.AddScoped<Akyildiz.Sevkiyat.Application.Reconciliation.Services.ReconciliationGuard>();

// Excel Service
builder.Services.AddScoped<Akyildiz.Sevkiyat.Application.Interfaces.IStockCountExcelService, Akyildiz.Sevkiyat.Infrastructure.Excel.ClosedXmlStockCountExcelService>();

// Pre-dispatch enforcement
builder.Services.AddScoped<Akyildiz.Sevkiyat.Application.Warehouse.Services.PreDispatchGuard>();

// Configure SeedData Options
builder.Services.AddOptions<SeedDataOptions>()
    .Bind(builder.Configuration.GetSection("SeedData"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() 
            ?? throw new InvalidOperationException("Jwt configuration is missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOpt.Issuer,
            ValidAudience = jwtOpt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOpt.Key))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var payload = new { type = "unauthorized", message = "Yetkisiz erişim. Lütfen giriş yapın.", traceId = context.HttpContext.TraceIdentifier };
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
                }
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var payload = new { type = "forbidden", message = "Bu işlemi yapmaya yetkiniz yok.", traceId = context.HttpContext.TraceIdentifier };
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
            }
        };
    });

builder.Services.AddAuthorization();

// Rate Limiting — Login endpoint'ini brute-force'a karşı koru
builder.Services.AddRateLimiter(rateLimiter =>
{
    rateLimiter.AddFixedWindowLimiter("login", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(15);
        options.QueueLimit = 0;
    });
    rateLimiter.AddFixedWindowLimiter("optimize", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    rateLimiter.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var payload = new { type = "rate_limit_exceeded", message = "Çok fazla giriş denemesi yapıldı. 15 dakika sonra tekrar deneyin." };
        await context.HttpContext.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(payload,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
    };
});

// DbContext
var connectionString = builder.Configuration.GetConnectionString("SevkiyatConnection");
builder.Services.AddDbContext<SevkiyatDbContext>(options => options.UseSqlServer(connectionString));

// IApplicationDbContext mapping
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SevkiyatDbContext>());

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Security Placeholder Validation ---
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var sensitiveConfigs = new Dictionary<string, string?>
{
    { "ConnectionStrings:SevkiyatConnection", app.Configuration.GetConnectionString("SevkiyatConnection") },
    { "Jwt:Key", app.Services.GetRequiredService<IOptions<JwtOptions>>().Value.Key },
    { "ISSIp:KullaniciAdi", app.Services.GetRequiredService<IOptions<ISSIpOptions>>().Value.KullaniciAdi },
    { "ISSIp:Sifre", app.Services.GetRequiredService<IOptions<ISSIpOptions>>().Value.Sifre },
    { "ISSIp:BasicAuthUsername", app.Services.GetRequiredService<IOptions<ISSIpOptions>>().Value.BasicAuthUsername },
    { "ISSIp:BasicAuthPassword", app.Services.GetRequiredService<IOptions<ISSIpOptions>>().Value.BasicAuthPassword },
    { "SeedData:AdminPassword", app.Services.GetRequiredService<IOptions<SeedDataOptions>>().Value.AdminPassword },
    { "Smtp:Host", app.Services.GetRequiredService<IOptions<SmtpOptions>>().Value.Host },
    { "Smtp:UserName", app.Services.GetRequiredService<IOptions<SmtpOptions>>().Value.UserName },
    { "Smtp:Password", app.Services.GetRequiredService<IOptions<SmtpOptions>>().Value.Password },
    { "Smtp:FromAddress", app.Services.GetRequiredService<IOptions<SmtpOptions>>().Value.FromAddress },
    { "Netsis:BaseUrl",      app.Services.GetRequiredService<IOptions<NetsisOptions>>().Value.BaseUrl },
    { "Netsis:KullaniciAdi", app.Services.GetRequiredService<IOptions<NetsisOptions>>().Value.KullaniciAdi },
    { "Netsis:Sifre",        app.Services.GetRequiredService<IOptions<NetsisOptions>>().Value.Sifre },
    { "Netsis:FirmaKodu",    app.Services.GetRequiredService<IOptions<NetsisOptions>>().Value.FirmaKodu },
    { "Netsis:SubeKodu",     app.Services.GetRequiredService<IOptions<NetsisOptions>>().Value.SubeKodu },
};

foreach (var config in sensitiveConfigs)
{
    if (string.IsNullOrWhiteSpace(config.Value) || config.Value.StartsWith("SET_BY_ENV_"))
    {
        logger.LogCritical("Security Refactor: Critical configuration '{Key}' is missing or still set to a placeholder. Application shutting down.", config.Key);
        throw new InvalidOperationException($"Critical configuration '{config.Key}' is not configured. Please set the corresponding environment variable.");
    }
}
// ---------------------------------------

app.UseHttpsRedirection();

// ✅ CORS (MapControllers'dan önce)
app.UseCors("AllowVueApp");

// ✅ Exception middleware her ortamda en üstte
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



// Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SevkiyatDbContext>();
    var hasher = services.GetRequiredService<IPasswordHasher>();
    
    // Ensure database is created/migrated
    // Note: In production better to use migration scripts, but for this project:
    context.Database.Migrate();

    var seedOpt = services.GetRequiredService<IOptions<SeedDataOptions>>().Value;
    await Akyildiz.Sevkiyat.Infrastructure.Persistence.Seeding.UserSeeder.SeedAsync(context, hasher, seedOpt.AdminPassword);
    await Akyildiz.Sevkiyat.Infrastructure.Persistence.Seeding.ShipmentSeeder.SeedAsync(context);
}

app.MapControllers();
app.Run();
