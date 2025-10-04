using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;

// Servicios propios
using PortalAcademico.Services; // ICursoService, CursoService

// Redis
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// DB: SQLite local / Postgres en prod (Host=)
// ==============================================
var cs = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(cs) &&
        cs.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        // Requiere Npgsql.EntityFrameworkCore.PostgreSQL 8.x
        options.UseNpgsql(cs);
        Console.WriteLine("💽 DB: PostgreSQL (por cadena con Host=).");
    }
    else
    {
        options.UseSqlite(cs);
        Console.WriteLine("💽 DB: SQLite.");
    }
});

// ==============================================
// Identity + Roles + Cookie (AccessDenied)
// ==============================================
builder.Services
    .AddDefaultIdentity<IdentityUser>(opts =>
    {
        opts.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath        = "/Identity/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // UI de Identity

// ==============================================
// REDIS (prioriza Redis__ConnectionString) + fallback a memoria
// ==============================================
try
{
    // 1) Rúbrica: una sola cadena (Upstash, etc.)
    var redisConnStr = builder.Configuration["Redis:ConnectionString"]
                       ?? builder.Configuration["Redis__ConnectionString"];

    ConfigurationOptions options;

    if (!string.IsNullOrWhiteSpace(redisConnStr))
    {
        options = ConfigurationOptions.Parse(redisConnStr);
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 5000;
        options.SyncTimeout = 5000;

        Console.WriteLine("🔗 Redis: usando Redis__ConnectionString.");
    }
    else
    {
        // 2) Estilo por partes (Redis:Host/Port/User/Password)
        var host = builder.Configuration["Redis:Host"] ?? builder.Configuration["Redis__Host"] ?? "127.0.0.1";
        int port = builder.Configuration.GetValue<int?>("Redis:Port")
                  ?? builder.Configuration.GetValue<int?>("Redis__Port")
                  ?? 6379; // ✅ int, sin .Value
        var user = builder.Configuration["Redis:User"] ?? builder.Configuration["Redis__User"];
        var pass = builder.Configuration["Redis:Password"] ?? builder.Configuration["Redis__Password"];

        options = new ConfigurationOptions
        {
            AbortOnConnectFail = false, // en cloud no abortar para reconectar
            ConnectTimeout = 5000,
            SyncTimeout = 5000
        };
        options.EndPoints.Add(host, port); // ✅ sin .Value
        if (!string.IsNullOrWhiteSpace(user)) options.User = user;
        if (!string.IsNullOrWhiteSpace(pass)) options.Password = pass;

        Console.WriteLine($"🔗 Redis: {host}:{port}");
    }

    var mux  = await ConnectionMultiplexer.ConnectAsync(options);
    var ping = await mux.GetDatabase().PingAsync();

    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    builder.Services.AddStackExchangeRedisCache(o =>
    {
        o.ConfigurationOptions = options;
        o.InstanceName = "portalacademico:";
    });

    Console.WriteLine($"✅ Redis OK (PING {ping.TotalMilliseconds:n0} ms).");
}
catch (Exception ex)
{
    // Fallback real: memoria
    builder.Services.AddDistributedMemoryCache();
    Console.WriteLine($"⚠️ Redis no disponible, usando memoria: {ex.Message}");
}

// ==============================================
// Session (toma Redis si está, memoria si no)
// ==============================================
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".PortalAcademico.Session";
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// Utilidades / DI de servicios
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICursoService, CursoService>();

// ==============================================
// Build
// ==============================================
var app = builder.Build();

// ==============================================
// Pipeline
// ==============================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// ==============================================
// (Opcional) Migraciones al arrancar (Postgres/SQLite)
// ==============================================
try
{
    using var migrateScope = app.Services.CreateScope();
    var db = migrateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    Console.WriteLine("✅ Migrations applied.");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Migrations fallaron: {ex.GetBaseException().Message}");
}

// ==============================================
// Seeder con protección
// ==============================================
try
{
    using var scope = app.Services.CreateScope();
    await DataSeeder.SeedAsync(scope.ServiceProvider);
    Console.WriteLine("✅ Seeder OK.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Seeder FALLÓ: {ex.GetType().Name} - {ex.Message}");
    // No tumbamos la app por seed.
}

// ==============================================
// Endpoints
// ==============================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
