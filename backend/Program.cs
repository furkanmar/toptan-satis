using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using WholesaleApi.Data;
using WholesaleApi.Middleware;
using WholesaleApi.Services;
using WholesaleApi.Services.Storage;

// Npgsql 6+: DateTime.Unspecified → timestamp with time zone uyumu
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Services ───────────────────────────────────────────────────────────────

// AuditInterceptor singleton — IHttpContextAccessor'ı AsyncLocal ile kullanır
builder.Services.AddSingleton<AuditInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
       .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

// JWT
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "Jwt:Secret eksik. Ortam değişkeni Jwt__Secret ayarlanmış olmalı.");
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

// ─── File Storage (R2) ──────────────────────────────────────────────────────
var r2Options = new R2StorageOptions
{
    AccountId    = builder.Configuration["R2:AccountId"]    ?? throw new InvalidOperationException("R2:AccountId eksik"),
    AccessKeyId  = builder.Configuration["R2:AccessKeyId"]  ?? throw new InvalidOperationException("R2:AccessKeyId eksik"),
    SecretAccessKey = builder.Configuration["R2:SecretAccessKey"] ?? throw new InvalidOperationException("R2:SecretAccessKey eksik"),
    BucketName   = builder.Configuration["R2:BucketName"]   ?? "marifoglu-media"
};

builder.Services.AddSingleton(r2Options);
builder.Services.AddSingleton<R2FileStorage>();
builder.Services.AddSingleton<IFileStorageService>(sp => sp.GetRequiredService<R2FileStorage>());

// LocalFileStorage — migration script + legacy dosya silme için
builder.Services.AddSingleton<LocalFileStorage>(sp =>
{
    var env2 = sp.GetRequiredService<IWebHostEnvironment>();
    // BaseUrl runtime'da değişkendir; migration için sabit değil, sadece legacy delete kullanılıyor
    return new LocalFileStorage(env2.WebRootPath, "http://localhost");
});

// App services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<OrderService>();

// CORS — React dev + production
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",           // Vite dev
                "https://wholesale.marifoglu.trade" // production
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Wholesale API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Static files for uploaded images
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ─── Middleware ──────────────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseStaticFiles(); // serves wwwroot/uploads
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── Auto migrate on startup ─────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Log.Information("Database migrated");
    await DbSeeder.SeedAsync(db);
    Log.Information("Database seeded");
}

await app.RunAsync();
