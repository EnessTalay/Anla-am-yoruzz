using Anlasalamiyoruz.Application;
using Anlasalamiyoruz.Infrastructure;
using Anlasalamiyoruz.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Anlasalamiyoruz API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // ── Configuration Validation ──────────────────────────────────────────────
    // Değerler yoksa uygulama çökmez; sadece uyarı loglanır.
    // Set via env vars: ConnectionStrings__DefaultConnection  /  Gemini__ApiKey
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        Log.Warning("Configuration missing: 'ConnectionStrings:DefaultConnection' is not set. " +
                    "Database features will be unavailable. " +
                    "Set the 'ConnectionStrings__DefaultConnection' environment variable.");

    if (string.IsNullOrWhiteSpace(builder.Configuration["Gemini:ApiKey"]))
        Log.Warning("Configuration missing: 'Gemini:ApiKey' is not set. " +
                    "AI analysis will use fallback responses only. " +
                    "Set the 'Gemini__ApiKey' environment variable.");

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // ── CORS ──────────────────────────────────────────────────────────────────
    // Set via env var: Cors__AllowedOrigins=https://anlasamiyoruz.vercel.app
    var corsOriginsRaw = builder.Configuration["Cors:AllowedOrigins"];
    if (string.IsNullOrWhiteSpace(corsOriginsRaw))
        Log.Warning("Configuration missing: 'Cors:AllowedOrigins' is not set. " +
                    "Defaulting to localhost origins.");

    var allowedOrigins = (corsOriginsRaw ?? "http://localhost:5173,http://localhost:3000")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    Log.Information("CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    builder.Services.AddControllers();
    builder.Services.Configure<FormOptions>(x => x.ValueCountLimit = 100);
    builder.Services.AddHttpClient(string.Empty, c => c.Timeout = TimeSpan.FromSeconds(100));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Anlasalamiyoruz API", Version = "v1" });
    });

    var app = builder.Build();

    // ── Auto Migration Runner ─────────────────────────────────────────────────
    // Uygulama ayağa kalktığında bekleyen migration'ları otomatik uygular.
    // DB bağlantısı yoksa ya da migration başarısız olursa uygulama çökmez;
    // hata loglanır ve devam edilir.
    using (var scope = app.Services.CreateScope())
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Log.Warning("Migration skipped: No database connection string is configured.");
        }
        else
        {
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var pending = db.Database.GetPendingMigrations().ToList();

                if (pending.Count == 0)
                {
                    Log.Information("Database is up-to-date. No pending migrations.");
                }
                else
                {
                    Log.Information("Applying {Count} pending migration(s): {Migrations}",
                        pending.Count, string.Join(", ", pending));
                    db.Database.Migrate();
                    Log.Information("Database migration completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database migration failed. " +
                              "The application will continue but database features may be unavailable.");
            }
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseCors("AllowFrontend");

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
