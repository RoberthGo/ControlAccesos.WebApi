﻿namespace ControlAccesos.WebApi.DTOs
{
    public class AccessRecordDto
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public string TipoAcceso { get; set; }
        public string? NombreResidente { get; set; }
        public string? ApellidosResidente { get; set; }
        public string? DomicilioResidente { get; set; } // Información adicional del residente
        public string? NombreInvitado { get; set; }
        public string? ApellidosInvitado { get; set; }
        public string? TipoInvitacionInvitado { get; set; } // Información adicional del invitado
        public string? NombreGuardia { get; set; }
        public string? ApellidosGuardia { get; set; }
        public string? PlacasVehiculo { get; set; }
        public string? Notas { get; set; }
    }
}
