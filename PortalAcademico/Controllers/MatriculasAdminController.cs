using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;

namespace PortalAcademico.Controllers
{
    [Authorize(Roles = "Coordinador")]
    public class MatriculasAdminController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public MatriculasAdminController(ApplicationDbContext ctx) => _ctx = ctx;

        // GET: /MatriculasAdmin/PorCurso?cursoId=1
        [HttpGet]
        public async Task<IActionResult> PorCurso(int? cursoId)
        {
            var cursos = await _ctx.Cursos.AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewBag.Cursos = cursos;

            if (!cursoId.HasValue && cursos.Any())
                cursoId = cursos.First().Id;

            var mats = await _ctx.Matriculas
                .Include(m => m.Curso)
                .Include(m => m.Usuario) // navegación al usuario (Identity)
                .Where(m => !cursoId.HasValue || m.CursoId == cursoId.Value)
                .OrderByDescending(m => m.FechaRegistro)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.CursoId = cursoId;
            return View(mats);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(int id)
        {
            var m = await _ctx.Matriculas
                .Include(x => x.Curso)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (m is null) return NotFound();

            // Solo se confirma si está Pendiente
            if (m.Estado != EstadoMatricula.Pendiente)
            {
                TempData["error"] = "Solo se pueden confirmar matrículas en estado Pendiente.";
                return RedirectToAction(nameof(PorCurso), new { cursoId = m.CursoId });
            }

            // Validar cupo
            var confirmadas = await _ctx.Matriculas
                .CountAsync(x => x.CursoId == m.CursoId && x.Estado == EstadoMatricula.Confirmada);

            if (confirmadas >= m.Curso.CupoMaximo)
            {
                TempData["error"] = "No hay cupos disponibles.";
                return RedirectToAction(nameof(PorCurso), new { cursoId = m.CursoId });
            }

            m.Estado = EstadoMatricula.Confirmada;
            await _ctx.SaveChangesAsync();
            TempData["ok"] = "Matrícula confirmada.";
            return RedirectToAction(nameof(PorCurso), new { cursoId = m.CursoId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var m = await _ctx.Matriculas.FindAsync(id);
            if (m is null) return NotFound();

            // Cancelar desde cualquier estado
            m.Estado = EstadoMatricula.Cancelada;
            await _ctx.SaveChangesAsync();
            TempData["ok"] = "Matrícula cancelada.";
            return RedirectToAction(nameof(PorCurso), new { cursoId = m.CursoId });
        }
    }
}
