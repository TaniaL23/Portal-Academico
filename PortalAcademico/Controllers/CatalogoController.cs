using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PortalAcademico.Services;          // ICursoService
using PortalAcademico.ViewModels;
using PortalAcademico.Models;            // Curso
using System.Globalization;

public class CatalogoController : Controller
{
    private readonly ICursoService _cursos;

    public CatalogoController(ICursoService cursos)
        => _cursos = cursos;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] CatalogoFiltrosVM filtros)
    {
        // 1) Trae TODOS los cursos ACTIVO desde cache (Redis 60s)
        var activos = await _cursos.GetActivosAsync(); // List<Curso>

        // 2) Validación de filtros
        if (!ModelState.IsValid)
        {
            return View(new CatalogoIndexVM
            {
                Filtros = filtros,
                Cursos  = Array.Empty<Curso>()
            });
        }

        // 3) Aplica filtros EN MEMORIA (IEnumerable) porque ya no es EF IQueryable
        IEnumerable<Curso> query = activos;

        if (!string.IsNullOrWhiteSpace(filtros.Nombre))
        {
            var patron = filtros.Nombre.Trim();
            query = query.Where(c =>
                (!string.IsNullOrEmpty(c.Nombre) && c.Nombre.Contains(patron, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.Codigo) && c.Codigo.Contains(patron, StringComparison.OrdinalIgnoreCase)));
        }

        if (filtros.CreditosMin.HasValue)
            query = query.Where(c => c.Creditos >= filtros.CreditosMin.Value);

        if (filtros.CreditosMax.HasValue)
            query = query.Where(c => c.Creditos <= filtros.CreditosMax.Value);

        if (filtros.HoraInicioDesde.HasValue)
            query = query.Where(c => c.HorarioInicio >= filtros.HoraInicioDesde.Value);

        if (filtros.HoraFinHasta.HasValue)
            query = query.Where(c => c.HorarioFin <= filtros.HoraFinHasta.Value);

        var cursosFiltrados = query
            .OrderBy(c => c.Nombre)
            .ToList();

        return View(new CatalogoIndexVM { Filtros = filtros, Cursos = cursosFiltrados });
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var curso = await _cursos.GetByIdAsync(id);
        if (curso == null || !curso.Activo) return NotFound();

        // 🧠 Sesión (Redis-backed): último curso visitado
        HttpContext.Session.SetInt32("LastCourseId", curso.Id);
        HttpContext.Session.SetString("LastCourseName", curso.Nombre ?? $"#{curso.Id}");

        return View(curso);
    }

    // Si más adelante agregas Crear/Editar, recuerda invalidar:
    // await _cursos.InvalidateActivosCacheAsync();
}
