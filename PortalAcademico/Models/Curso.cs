using System.ComponentModel.DataAnnotations;

namespace PortalAcademico.Models
{
    public class Curso : IValidatableObject
    {
        public int Id { get; set; }

        [Required, MaxLength(10)]
        public string Codigo { get; set; } = "";

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = "";

        [Range(1, 30, ErrorMessage = "Créditos debe ser > 0")]
        public int Creditos { get; set; }

        [Range(1, 1000)]
        public int CupoMaximo { get; set; }

        [Required]
        public TimeOnly HorarioInicio { get; set; }

        [Required]
        public TimeOnly HorarioFin { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<Matricula> Matriculas { get; set; } = new List<Matricula>();

        // Regla: HorarioInicio < HorarioFin
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HorarioInicio >= HorarioFin)
            {
                yield return new ValidationResult(
                    "HorarioInicio debe ser menor que HorarioFin",
                    new[] { nameof(HorarioInicio), nameof(HorarioFin) });
            }
        }
    }
}
