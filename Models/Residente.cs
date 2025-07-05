using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlAccesos.WebApi.Models
{
    [Table("Residentes")]
    public class Residente
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string Apellidos { get; set; }

        [StringLength(200)]
        public string Domicilio { get; set; }

        [StringLength(20)]
        public string Telefono { get; set; }

        [StringLength(100)]
        public string Vehiculo { get; set; }

        [StringLength(20)]
        public string Placas { get; set; }

        public int UserId { get; set; } 

        [ForeignKey("UserId")] // Define la clave foránea
        public Usuario Usuario { get; set; } // Propiedad de navegación

        public ICollection<Invitado> Invitados { get; set; }
        public ICollection<RegistroAcceso> RegistrosAcceso { get; set; }
    }
    
}
