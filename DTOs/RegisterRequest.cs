using System.ComponentModel.DataAnnotations;

namespace ControlAccesos.WebApi.DTOs
{
    public class RegisterRequest : IValidatableObject
    {
        // --- Información del Usuario (para la tabla Usuarios) ---
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [StringLength(256, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "El rol es requerido.")]
        [StringLength(20, ErrorMessage = "El rol no puede exceder los 20 caracteres.")]
        public string Rol { get; set; } // Ej. "Residente", "Guardia"

        // --- Información Personal (para tablas Residentes o Guardias) ---
        // Estos campos no tienen [Required] aquí, se validarán condicionalmente en Validate()
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string Apellidos { get; set; }

        [StringLength(20)]
        public string Placas { get; set; }

        // Campos específicos para Residente
        [StringLength(200)]
        public string Domicilio { get; set; }

        [StringLength(20)]
        public string Telefono { get; set; }

        [StringLength(100)]
        public string Vehiculo { get; set; }

        
        // Método para la validación condicional
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validar que el rol sea uno de los permitidos
            if (Rol != "Residente" && Rol != "Guardia")
            {
                yield return new ValidationResult(
                    "Rol inválido. Los roles permitidos son 'Residente' o 'Guardia'.",
                    new[] { nameof(Rol) });
            }

            if (string.IsNullOrWhiteSpace(Nombre))
                yield return new ValidationResult("Para el rol '"+Rol+"', el Nombre es requerido.", new[] { nameof(Nombre) });
            if (string.IsNullOrWhiteSpace(Apellidos))
                yield return new ValidationResult("Para el rol '"+Rol+"', los Apellidos son requeridos.", new[] { nameof(Apellidos) });
        }

    }
}
