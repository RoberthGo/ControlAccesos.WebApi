namespace ControlAccesos.WebApi.DTOs
{
    public class AccessHistoryRequest
    {
        public DateTime? FechaInicio { get; set; } // Opcional: Fecha y hora de inicio del rango
        public DateTime? FechaFin { get; set; }   // Opcional: Fecha y hora de fin del rango
        public int? ResidenteId { get; set; }     // Opcional: Filtrar por ID de residente
        public int? InvitadoId { get; set; }      // Opcional: Filtrar por ID de invitado
        public string? TipoAcceso { get; set; }   // Opcional: "Entrada" o "Salida"
        public int? GuardiaId { get; set; }       // Opcional: Filtrar por ID de guardia
        public string? PlacasVehiculo { get; set; } // Opcional: Filtrar por placas de vehículo
        public string? tipoDePersona { get; set; }
    }
}
