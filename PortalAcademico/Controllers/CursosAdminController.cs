using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;
using PortalAcademico.Services; // ICursoService

namespace PortalAcademico.Controllers
{
    [Authorize(Roles = "Coordinador")]
    public class CursosAdminController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ICursoService _cursos;

        public CursosAdminController(ApplicationDbContext ctx, ICursoService cursos)
        {
            _ctx = ctx;
            _cursos = cursos;
        }

        // GET: /CursosAdmin
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cursos = await _ctx.Cursos
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(cursos);
        }

        // GET: /CursosAdmin/Crear
        [HttpGet]
        public IActionResult Crear() => View(new Curso { Activo = true });

        // POST: /CursosAdmin/Crear
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Curso curso)
        {
            if (!ModelState.IsValid) return View(curso);

            _ctx.Add(curso);
            await _ctx.SaveChangesAsync();
            await _cursos.InvalidateActivosCacheAsync();   // P4: invalidar cache

            TempData["ok"] = "Curso creado.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /CursosAdmin/Editar/5
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var curso = await _ctx.Cursos.FindAsync(id);
            if (curso is null) return NotFound();
            return View(curso);
        }

        // POST: /CursosAdmin/Editar/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Curso form)
        {
            if (id != form.Id) return NotFound();

            // Si CupoMaximo no se edita en la vista, puedes quitar esta línea.
            // ModelState.Remove(nameof(Curso.CupoMaximo));

            if (!ModelState.IsValid) return View(form);

            var db = await _ctx.Cursos.FirstOrDefaultAsync(x => x.Id == id);
            if (db is null) return NotFound();

            // Mapeo explícito (evita overposting) — INCLUYE Activo ✔
            db.Codigo        = form.Codigo;
            db.Nombre        = form.Nombre;
            db.Creditos      = form.Creditos;
            db.CupoMaximo    = form.CupoMaximo;
            db.HorarioInicio = form.HorarioInicio;
            db.HorarioFin    = form.HorarioFin;
            db.Activo        = form.Activo;

            try
            {
                await _ctx.SaveChangesAsync();
                await _cursos.InvalidateActivosCacheAsync();   // P4: invalidar cache
                TempData["ok"] = "Curso actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Si alguien modificó el registro entre GET y POST
                ModelState.AddModelError(string.Empty, "El curso fue modificado por otro usuario. Vuelve a intentar.");
                return View(form);
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, $"No se pudo guardar los cambios: {ex.GetBaseException().Message}");
                return View(form);
            }
        }

        // POST: /CursosAdmin/Desactivar/5 (soft-delete)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var curso = await _ctx.Cursos.FindAsync(id);
            if (curso is null) return NotFound();

            if (!curso.Activo)
            {
                TempData["ok"] = "El curso ya estaba desactivado.";
                return RedirectToAction(nameof(Index));
            }

            curso.Activo = false;

            await _ctx.SaveChangesAsync();
            await _cursos.InvalidateActivosCacheAsync();   // P4: invalidar cache

            TempData["ok"] = "Curso desactivado.";
            return RedirectToAction(nameof(Index));
        }
    }
}
