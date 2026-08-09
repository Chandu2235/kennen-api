using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Kennen.Api.Auth;
using Kennen.Api.Extensions;
using Kennen.Api.Storage;
using Kennen.Infrastructure.Identity;
using Kennen.Infrastructure.Persistence;
using Kennen.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// Ensure the web root and admin SPA directory exist before the static file
// provider is initialised at application startup.
var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var adminRoot = Path.Combine(webRoot, "admin");
Directory.CreateDirectory(adminRoot);

var builder = WebApplication.CreateBuilder(args);
builder.Environment.WebRootPath = webRoot;
builder.Environment.WebRootFileProvider = new PhysicalFileProvider(webRoot);

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    // Fail fast on a missing or too-short signing key rather than at first login.
    .ValidateOnStart();

builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
var seedOptions = builder.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();
builder.Services.AddSingleton(seedOptions);

// ---------------------------------------------------------------------------
// Persistence
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

builder.Services.AddDbContext<KennenDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
    }
});

builder.Services.AddScoped<DbSeeder>();

// ---------------------------------------------------------------------------
// Identity + JWT
// ---------------------------------------------------------------------------
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<KennenDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

// ---------------------------------------------------------------------------
// CORS - the static Vercel frontend calls this API cross-origin
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsSettings.PolicyName, policy =>
    {
        if (builder.Environment.IsDevelopment() && corsSettings.AllowedOrigins.Length == 0)
        {
            // In development, the admin preview uses a dynamic tunnel origin, so
            // allow any origin. JWT is transmitted in the Authorization header, not
            // cookies, so this does not weaken our auth model.
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        if (corsSettings.AllowedOrigins.Length == 0)
        {
            // No origins configured: allow nothing rather than silently allowing everything.
            policy.WithOrigins(Array.Empty<string>());
            return;
        }

        policy.WithOrigins(corsSettings.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---------------------------------------------------------------------------
// Rate limiting for anonymous endpoints
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.PublicWrite, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10)
            }));

    options.AddPolicy(RateLimitPolicies.Authentication, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5)
            }));

    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            title = "Too many requests",
            detail = "You have made too many requests. Please wait a moment and try again.",
            status = StatusCodes.Status429TooManyRequests
        }, ct);
    };
});

// ---------------------------------------------------------------------------
// MVC, problem details, health checks, Swagger
// ---------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums travel as readable strings ("New", "FullTime") so the frontend never
        // depends on numeric ordering.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddResponseCaching();
builder.Services.AddHealthChecks().AddDbContextCheck<KennenDbContext>("database");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Kennen Technologies API",
        Version = "v1",
        Description = "Backend for kennen-technologies.com: contact intake, site content, careers and staff administration."
    });

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by POST /api/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kennen API v1"));
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Behind a reverse proxy / PaaS load balancer, trust the forwarded scheme and client IP
// so rate limiting partitions on the real caller rather than the proxy.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseCors(CorsSettings.PolicyName);
app.UseResponseCaching();
app.UseRateLimiter();

// Static admin SPA is served from wwwroot/admin under the /admin route. The assets
// contain no secrets; all data endpoints are protected by JWT. The client itself
// reroutes to the login view when no token is present.
var adminFiles = new PhysicalFileProvider(adminRoot);

app.UseDefaultFiles(new DefaultFilesOptions
{
    RequestPath = "/admin",
    FileProvider = adminFiles,
    DefaultFileNames = new List<string> { "index.html" }
});
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/admin",
    FileProvider = adminFiles
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// ---------------------------------------------------------------------------
// Database migration + seeding
// ---------------------------------------------------------------------------
if (builder.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KennenDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

app.Run();

/// <summary>
/// Partitions rate limits by authenticated user when available, falling back to remote IP
/// for anonymous callers.
/// </summary>
static string ClientKey(HttpContext context) =>
    context.User.Identity?.IsAuthenticated == true
        ? context.User.Identity!.Name ?? "authenticated"
        : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
