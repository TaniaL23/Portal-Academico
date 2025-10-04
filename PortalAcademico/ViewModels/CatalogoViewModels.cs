using System.ComponentModel.DataAnnotations;
using PortalAcademico.Models;
using System.Collections.Generic;

namespace PortalAcademico.ViewModels
{
    public class CatalogoFiltrosVM : IValidatableObject
    {
        [Display(Name = "Nombre contiene")]
        public string? Nombre { get; set; }

        [Display(Name = "Créditos mín.")]
        [Range(0, 100, ErrorMessage = "Créditos no puede ser negativo")]
        public int? CreditosMin { get; set; }

        [Display(Name = "Créditos máx.")]
        [Range(0, 100, ErrorMessage = "Créditos no puede ser negativo")]
        public int? CreditosMax { get; set; }

        [Display(Name = "Horario desde")]
        public TimeOnly? HoraInicioDesde { get; set; }

        [Display(Name = "Horario hasta")]
        public TimeOnly? HoraFinHasta { get; set; }

        // Validaciones de rango
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CreditosMin.HasValue && CreditosMax.HasValue && CreditosMin > CreditosMax)
            {
                yield return new ValidationResult(
                    "El rango de créditos es inválido (mínimo > máximo).",
                    new[] { nameof(CreditosMin), nameof(CreditosMax) });
            }

            if (HoraInicioDesde.HasValue && HoraFinHasta.HasValue && HoraFinHasta < HoraInicioDesde)
            {
                yield return new ValidationResult(
                    "El HorarioFin no puede ser anterior al HorarioInicio.",
                    new[] { nameof(HoraInicioDesde), nameof(HoraFinHasta) });
            }
        }
    }

    public class CatalogoIndexVM
    {
        public CatalogoFiltrosVM Filtros { get; set; } = new();
        public IEnumerable<Curso> Cursos { get; set; } = new List<Curso>();
    }
}
