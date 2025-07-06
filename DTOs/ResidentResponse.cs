namespace ControlAccesos.WebApi.DTOs
{
    public class ResidentResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string? Domicilio { get; set; }
        public string? Telefono { get; set; }
        public string? Vehiculo { get; set; }
        public string? Placas { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } 
    }
}
