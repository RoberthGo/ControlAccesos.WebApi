using System;
using System.ComponentModel.DataAnnotations;

namespace ControlAccesos.WebApi.DTOs
{
    public class CreateInvitadoRequest
    {
        [Required(ErrorMessage = "El nombre del invitado es requerido.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Los apellidos del invitado son requeridos.")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder los 100 caracteres.")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "El tipo de invitación es requerido.")]
        [StringLength(50, ErrorMessage = "El tipo de invitación no puede exceder los 50 caracteres.")]
        // Puede ser "Unica", "Recurrente", "PorFecha"
        public string TipoInvitacion { get; set; }

        // Fecha de validez, opcional si TipoInvitacion no es "PorFecha"
        public DateTime? FechaValidez { get; set; }

        // NOTA: El ResidenteId no se pide aquí, se obtendrá del JWT del usuario autenticado.
    }
}
