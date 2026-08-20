using System.Text;
using CMOmeets.Api.Auth;
using CMOmeets.Application.Auth;
using CMOmeets.Domain.Data;
using CMOmeets.Domain.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var cmoConnectionString = builder.Configuration.GetConnectionString("CMOmeets");
builder.Services.AddDbContext<CmoMeetsDbContext>(options =>
    options.UseSqlServer(cmoConnectionString, sql =>
    {
        
    }));

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<CmoMeetsDbContext>();

// Custom hasher validates legacy SHA-512 accounts and upgrades them on login.
builder.Services.AddScoped<IPasswordHasher<AppUser>, LegacyPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<CMOmeets.Application.Masters.MastersService>();
builder.Services.AddScoped<CMOmeets.Application.Meetings.MeetingsService>();
builder.Services.AddScoped<CMOmeets.Application.Agendas.AgendasService>();
builder.Services.AddScoped<CMOmeets.Application.Groups.GroupsService>();
builder.Services.AddScoped<CMOmeets.Application.Dashboard.DashboardService>();
builder.Services.AddScoped<CMOmeets.Application.Reports.ReportsService>();
builder.Services.AddSingleton<CMOmeets.Application.Files.AtrFileService>();
builder.Services.AddScoped<CMOmeets.Application.Users.UsersService>();
builder.Services.AddScoped<CMOmeets.Application.Search.SearchService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
    });

builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:7021; http://localhost:5000; http://localhost:4200" };
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Translate ownership-rule violations (thrown deep in services) into a clean 403.
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (UnauthorizedAccessException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ctx.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/health", async (CmoMeetsDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return Results.Ok(new { status = canConnect ? "healthy" : "degraded", database = canConnect });
});

// Make sure a LocalDB target is actually up before we touch it (idempotent; no-op
// for non-LocalDB connections), then auto-create/upgrade the schema on startup so
// pointing the connection string at a new or relocated database needs no manual work.
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    LocalDbStarter.EnsureStarted(cmoConnectionString, startupLogger);

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CmoMeetsDbContext>();

    // Retry the very first connection — a cold LocalDB instance can still be coming up.
    const int maxAttempts = 5;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            startupLogger.LogWarning(ex,
                "Database not ready (attempt {Attempt}/{Max}); retrying in 3s…", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

await IdentitySeeder.SeedAsync(app.Services);

app.Run();
