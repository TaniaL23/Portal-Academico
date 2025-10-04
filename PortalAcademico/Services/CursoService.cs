using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

using PortalAcademico.Data;
using PortalAcademico.Models;

namespace PortalAcademico.Services
{
    public interface ICursoService
    {
        Task<List<Curso>> GetActivosAsync();
        Task<Curso?> GetByIdAsync(int id);
        Task InvalidateActivosCacheAsync();
    }

    public class CursoService : ICursoService
    {
        private readonly ApplicationDbContext _db;
        private readonly IDatabase? _redis;               // <- puede ser null si no hay Redis
        private readonly bool _hasRedis;
        private readonly RedisConfiguration _cfg;
        private readonly ILogger<CursoService> _log;

        private const string ACTIVES_KEY = "cursos:activos:all";

        public CursoService(
            ApplicationDbContext db,
            IServiceProvider sp,                           // <- Pedimos el SP, no el multiplexer directo
            IOptions<RedisConfiguration> cfg,
            ILogger<CursoService> log)
        {
            _db  = db;
            _cfg = cfg.Value;
            _log = log;

            // Intentamos obtener IConnectionMultiplexer solo si fue registrado (cuando hay Redis)
            var mux = sp.GetService<IConnectionMultiplexer>();
            if (mux != null)
            {
                _redis = mux.GetDatabase();
                _hasRedis = true;
            }
            else
            {
                _hasRedis = false;
            }
        }

        public async Task<List<Curso>> GetActivosAsync()
        {
            // 1) Intentar Redis solo si está disponible
            if (_hasRedis && _redis != null)
            {
                try
                {
                    var cached = await _redis.StringGetAsync(ACTIVES_KEY);
                    if (cached.HasValue)
                    {
                        _log.LogInformation("Cursos activos desde Redis (HIT).");
                        return JsonSerializer.Deserialize<List<Curso>>(cached!) ?? new();
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "No se pudo leer cursos activos desde Redis. Continuando con DB.");
                }
            }

            // 2) DB (y si hay Redis, setear cache)
            _log.LogInformation("Cache MISS. Leyendo cursos activos desde DB.");
            var data = await _db.Cursos
                                .AsNoTracking()
                                .Where(c => c.Activo)
                                .OrderBy(c => c.Nombre)
                                .ToListAsync();

            if (_hasRedis && _redis != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(data);
                    var ttl  = TimeSpan.FromMinutes(_cfg.CacheTtlMinutes);
                    await _redis.StringSetAsync(ACTIVES_KEY, json, ttl);
                    _log.LogInformation("Cacheados {count} cursos activos (TTL {ttl} min).", data.Count, _cfg.CacheTtlMinutes);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "No se pudo escribir cursos activos en Redis.");
                }
            }

            return data;
        }

        public async Task<Curso?> GetByIdAsync(int id)
        {
            return await _db.Cursos
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task InvalidateActivosCacheAsync()
        {
            if (_hasRedis && _redis != null)
            {
                try
                {
                    await _redis.KeyDeleteAsync(ACTIVES_KEY);
                    _log.LogInformation("Invalidada la cache de cursos activos.");
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "No se pudo invalidar la cache en Redis.");
                }
            }
            else
            {
                // No hay Redis: no hay nada que invalidar (la cache en memoria del listado no la estamos usando aquí)
                _log.LogInformation("Sin Redis: no hay clave de cache que invalidar.");
            }
        }
    }
}
