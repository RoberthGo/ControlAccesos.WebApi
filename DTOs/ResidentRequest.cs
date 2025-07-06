using System.ComponentModel.DataAnnotations;

namespace ControlAccesos.WebApi.DTOs
{
    public class ResidentRequest
    {
        [Required(ErrorMessage = "El nombre del residente es requerido.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Los apellidos del residente son requeridos.")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder los 100 caracteres.")]
        public string Apellidos { get; set; }

        [StringLength(200, ErrorMessage = "El domicilio no puede exceder los 200 caracteres.")]
        public string? Domicilio { get; set; } // Nullable en la BD

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres.")]
        public string? Telefono { get; set; } // Nullable en la BD

        [StringLength(100, ErrorMessage = "El vehículo no puede exceder los 100 caracteres.")]
        public string? Vehiculo { get; set; } // Nullable en la BD

        [StringLength(20, ErrorMessage = "Las placas no pueden exceder los 20 caracteres.")]
        public string? Placas { get; set; } // Nullable en la BD
    }
}
