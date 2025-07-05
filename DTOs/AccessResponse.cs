namespace ControlAccesos.WebApi.DTOs
{
    public class AccessResponse
    {
        public int RegistroId { get; set; }
        public DateTime FechaHora { get; set; }
        public string TipoAcceso { get; set; }
        public string IdentificadorAcceso { get; set; } // QrCode o Placas
        public string NombrePersona { get; set; } // Nombre del residente o invitado
        public string ApellidosPersona { get; set; } // Apellidos del residente o invitado
        public string RolPersona { get; set; } // "Residente" o "Invitado"
        public string NombreGuardia { get; set; } // Nombre del guardia que registró
        public string PlacasVehiculo { get; set; }
        public string Mensaje { get; set; }
    }
}
