using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;

// 👇 Agregados
using StackExchange.Redis;
using PortalAcademico.Services; // RedisConfiguration, ICursoService, CursoService
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------
// DB
// -------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// -------------------------------------
// Identity + UI
// -------------------------------------
builder.Services
    .AddDefaultIdentity<IdentityUser>(opts =>
    {
        opts.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Para páginas de Identity

// -------------------------------------
// 🔴 REDIS: Config + Cache + Sesión con PING + fallback real
// -------------------------------------
builder.Services.Configure<RedisConfiguration>(
    builder.Configuration.GetSection("Redis"));

try
{
    // Leemos la config y probamos conexión AHORA (no diferida)
    var cfg = builder.Configuration.GetSection("Redis").Get<RedisConfiguration>()!;
    var options = new ConfigurationOptions
    {
        EndPoints = { { string.IsNullOrWhiteSpace(cfg.Host) ? "127.0.0.1" : cfg.Host, cfg.Port } },
        User = string.IsNullOrWhiteSpace(cfg.User) ? null : cfg.User,
        Password = string.IsNullOrWhiteSpace(cfg.Password) ? null : cfg.Password,
        AbortOnConnectFail = true,  // si no conecta, que falle aquí y caemos a memoria
        ConnectTimeout = 3000,
        SyncTimeout = 3000,
        ConnectRetry = 1
    };

    // Conectar y PING inmediato (si no hay Redis, lanzará excepción)
    var mux = await ConnectionMultiplexer.ConnectAsync(options);
    var db  = mux.GetDatabase();
    var ping = db.Ping(); // test rápido

    // Si llegamos aquí, Redis funciona: registramos servicios
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    builder.Services.AddStackExchangeRedisCache(o =>
    {
        o.Configuration = $"{cfg.Host}:{cfg.Port},user={cfg.User},password={cfg.Password}";
        o.InstanceName  = "portalacademico:";
    });

    Console.WriteLine($"✅ Redis configurado (PING {ping.TotalMilliseconds:n0} ms).");
}
catch (Exception ex)
{
    // Fallback: cache distribuida en memoria (no se registran servicios de Redis)
    builder.Services.AddDistributedMemoryCache();
    Console.WriteLine($"⚠️ Redis no disponible, usando memoria: {ex.Message}");
}

// Session (tomará IDistributedCache: Redis si está registrado; memoria si no)
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".PortalAcademico.Session";
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// Acceso a HttpContext en vistas/layout
builder.Services.AddHttpContextAccessor();

// Servicio de Cursos (cache + invalidación)
builder.Services.AddScoped<ICursoService, CursoService>();

// -------------------------------------
// Build
// -------------------------------------
var app = builder.Build();

// -------------------------------------
// Middleware pipeline
// -------------------------------------
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

// -------------------------------------
// Seeder con protección (para que no tumbe el arranque)
// -------------------------------------
try
{
    using var scope = app.Services.CreateScope();
    await DataSeeder.SeedAsync(scope.ServiceProvider);
    Console.WriteLine("✅ Seeder OK.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Seeder FALLÓ: {ex.GetType().Name} - {ex.Message}");
    // En dev no tumbamos la app.
}

// -------------------------------------
// Endpoints
// -------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
