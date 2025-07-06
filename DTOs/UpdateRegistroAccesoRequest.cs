using System.ComponentModel.DataAnnotations;

namespace ControlAccesos.WebApi.DTOs
{
    public class UpdateRegistroAccesoRequest
    {
        // Se hacen nullable para permitir actualizaciones parciales 
        [StringLength(20, ErrorMessage = "Las placas del vehículo no pueden exceder los 20 caracteres.")]
        public string? PlacasVehiculo { get; set; } // Placas del vehículo que está entrando/saliendo

        [StringLength(500, ErrorMessage = "Las notas no pueden exceder los 500 caracteres.")]
        public string? Notas { get; set; } // Notas adicionales del guardia
    }
}
