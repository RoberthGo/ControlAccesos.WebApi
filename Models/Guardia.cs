using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlAccesos.WebApi.Models
{
    [Table("Guardias")]
    public class Guardia
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string Apellidos { get; set; }

        [StringLength(20)]
        public string PlacasVehiculo { get; set; } // Placas del vehículo asignado al guardia (opcional)

        public int UserId { get; set; } 

        [ForeignKey("UserId")]
        public Usuario Usuario { get; set; }

        public ICollection<RegistroAcceso> RegistrosAcceso { get; set; }
    }
}
