using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;

[Authorize] // Usuario debe estar autenticado
public class MatriculasController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<IdentityUser> _userManager;

    public MatriculasController(ApplicationDbContext ctx, UserManager<IdentityUser> userManager)
    {
        _ctx = ctx;
        _userManager = userManager;
    }

    // GET: /Matriculas/Crear?cursoId=#
    [HttpGet]
    public async Task<IActionResult> Crear(int cursoId)
    {
        var curso = await _ctx.Cursos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cursoId && c.Activo);

        if (curso == null) return NotFound();

        ViewBag.Curso = curso;
        // La vista solo necesita CursoId como hidden
        return View(new Matricula { CursoId = cursoId });
    }

    // POST: /Matriculas/Crear
    // 🔒 Solo bindeamos CursoId (lo demás se asigna en servidor)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear([Bind(nameof(Matricula.CursoId))] Matricula model)
    {
        // Evitar que se validen campos que NO vienen del form
        ModelState.Remove(nameof(Matricula.UsuarioId));
        ModelState.Remove(nameof(Matricula.Usuario));
        ModelState.Remove(nameof(Matricula.Curso));

        var curso = await _ctx.Cursos
            .FirstOrDefaultAsync(c => c.Id == model.CursoId && c.Activo);
        if (curso == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Debes iniciar sesión para inscribirte.";
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        // ✅ Ya inscrito (no canceladas)
        bool yaInscrito = await _ctx.Matriculas
            .AnyAsync(m => m.CursoId == curso.Id &&
                           m.UsuarioId == userId &&
                           m.Estado != EstadoMatricula.Cancelada);
        if (yaInscrito)
            ModelState.AddModelError(string.Empty, "Ya estás inscrito en este curso.");

        // ✅ Cupo máximo (no canceladas)
        int ocupados = await _ctx.Matriculas
            .CountAsync(m => m.CursoId == curso.Id &&
                             m.Estado != EstadoMatricula.Cancelada);
        if (ocupados >= curso.CupoMaximo)
            ModelState.AddModelError(string.Empty, "El curso ya alcanzó su cupo máximo.");

        // ✅ Choque de horario (no canceladas)
        bool solapado = await _ctx.Matriculas
            .Include(m => m.Curso)
            .AnyAsync(m => m.UsuarioId == userId &&
                           m.Estado != EstadoMatricula.Cancelada &&
                           (curso.HorarioInicio < m.Curso.HorarioFin) &&
                           (curso.HorarioFin > m.Curso.HorarioInicio));
        if (solapado)
            ModelState.AddModelError(string.Empty, "Ya tienes otro curso en este horario.");

        if (!ModelState.IsValid)
        {
            // Volver a la vista con el curso y el hidden de CursoId
            ViewBag.Curso = curso;
            return View(new Matricula { CursoId = curso.Id });
        }

        // 🔁 Doble verificación justo antes de guardar (condición de carrera)
        ocupados = await _ctx.Matriculas
            .CountAsync(m => m.CursoId == curso.Id &&
                             m.Estado != EstadoMatricula.Cancelada);
        if (ocupados >= curso.CupoMaximo)
        {
            ViewBag.Curso = curso;
            ModelState.AddModelError(string.Empty, "El curso alcanzó su cupo mientras confirmabas. Intenta con otro.");
            return View(new Matricula { CursoId = curso.Id });
        }

        // ✅ Crear matrícula en estado Pendiente
        var nueva = new Matricula
        {
            CursoId       = curso.Id,
            UsuarioId     = userId,
            FechaRegistro = DateTime.UtcNow,
            Estado        = EstadoMatricula.Pendiente
        };

        _ctx.Matriculas.Add(nueva);

        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Probable índice único (CursoId, UsuarioId) o carrera
            ViewBag.Curso = curso;
            ModelState.AddModelError(string.Empty, "No pudimos completar la inscripción (posible duplicado o cupo).");
            return View(new Matricula { CursoId = curso.Id });
        }

        TempData["Success"] = $"Te inscribiste en {curso.Nombre}. Estado: Pendiente.";
        return RedirectToAction("Detalle", "Catalogo", new { id = curso.Id });
    }
}
