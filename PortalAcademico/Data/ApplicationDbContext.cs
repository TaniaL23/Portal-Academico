using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Models;

namespace PortalAcademico.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Curso> Cursos => Set<Curso>();
        public DbSet<Matricula> Matriculas => Set<Matricula>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Curso.Codigo único
            builder.Entity<Curso>()
                .HasIndex(c => c.Codigo)
                .IsUnique();

            // Check: Créditos > 0  (además del [Range] de DataAnnotations)
            builder.Entity<Curso>()
                .ToTable(t => t.HasCheckConstraint("CK_Curso_Creditos", "Creditos > 0"));

            // (Opcional) Check de horario — en SQLite compara HH:MM lexicográfico
            builder.Entity<Curso>()
                .ToTable(t => t.HasCheckConstraint("CK_Curso_Horario", "HorarioInicio < HorarioFin"));

            // Un usuario no puede matricularse dos veces al mismo curso
            builder.Entity<Matricula>()
                .HasIndex(m => new { m.CursoId, m.UsuarioId })
                .IsUnique();

            // Seed de 3 cursos activos (Ids fijos para el seed)
            builder.Entity<Curso>().HasData(
                new Curso {
                    Id = 1, Codigo = "IS101", Nombre = "Intro a Ing. Software",
                    Creditos = 3, CupoMaximo = 30,
                    HorarioInicio = new TimeOnly(8,0), HorarioFin = new TimeOnly(10,0),
                    Activo = true
                },
                new Curso {
                    Id = 2, Codigo = "BD201", Nombre = "Bases de Datos",
                    Creditos = 4, CupoMaximo = 25,
                    HorarioInicio = new TimeOnly(10,0), HorarioFin = new TimeOnly(12,0),
                    Activo = true
                },
                new Curso {
                    Id = 3, Codigo = "PR301", Nombre = ".NET Avanzado",
                    Creditos = 3, CupoMaximo = 20,
                    HorarioInicio = new TimeOnly(14,0), HorarioFin = new TimeOnly(16,0),
                    Activo = true
                }
            );
        }
    }
}
