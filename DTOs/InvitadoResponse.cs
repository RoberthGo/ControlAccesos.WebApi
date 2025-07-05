namespace ControlAccesos.WebApi.DTOs
{
    public class InvitadoResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string TipoInvitacion { get; set; }
        public DateTime? FechaValidez { get; set; }
        public string QrCode { get; set; } 
        public int ResidenteId { get; set; }
    }
}
