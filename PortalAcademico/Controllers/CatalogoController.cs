using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.ViewModels;

public class CatalogoController : Controller
{
    private readonly ApplicationDbContext _ctx;
    public CatalogoController(ApplicationDbContext ctx) => _ctx = ctx;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] CatalogoFiltrosVM filtros)
    {
        // Base: SOLO cursos activos
        var query = _ctx.Cursos.AsNoTracking().Where(c => c.Activo);

        // Valida DataAnnotations + IValidatableObject
        if (!ModelState.IsValid)
        {
            return View(new CatalogoIndexVM
            {
                Filtros = filtros,
                Cursos  = Array.Empty<PortalAcademico.Models.Curso>()
            });
        }

        // Aplica filtros SOLO si el modelo es válido
        if (!string.IsNullOrWhiteSpace(filtros.Nombre))
        {
            var patron = $"%{filtros.Nombre.Trim()}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Nombre, patron) ||
                EF.Functions.Like(c.Codigo, patron));
        }

        if (filtros.CreditosMin.HasValue)
            query = query.Where(c => c.Creditos >= filtros.CreditosMin.Value);

        if (filtros.CreditosMax.HasValue)
            query = query.Where(c => c.Creditos <= filtros.CreditosMax.Value);

        if (filtros.HoraInicioDesde.HasValue)
            query = query.Where(c => c.HorarioInicio >= filtros.HoraInicioDesde.Value);

        if (filtros.HoraFinHasta.HasValue)
            query = query.Where(c => c.HorarioFin <= filtros.HoraFinHasta.Value);

        var cursos = await query
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return View(new CatalogoIndexVM { Filtros = filtros, Cursos = cursos });
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var curso = await _ctx.Cursos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.Activo);

        if (curso == null) return NotFound();
        return View(curso);
    }
}
