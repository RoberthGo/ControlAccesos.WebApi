using System.ComponentModel.DataAnnotations;

namespace ControlAccesos.WebApi.DTOs
{
    public class UpdateInvitadoRequest
    {
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string? Nombre { get; set; } // Hacemos nullable para permitir actualizaciones parciales

        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder los 100 caracteres.")]
        public string? Apellidos { get; set; } // Hacemos nullable

        [StringLength(50, ErrorMessage = "El tipo de invitación no puede exceder los 50 caracteres.")]
        // Validar que sea "Unica" o "Recurrente" o "PorFecha"
        public string? TipoInvitacion { get; set; } // Hacemos nullable

        public DateTime? FechaValidez { get; set; } // Hacemos nullable
    }
}
