using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlAccesos.WebApi.Models
{
    [Table("Invitados")]
    public class Invitado
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string Apellidos { get; set; }

        [StringLength(50)]
        public string TipoInvitacion { get; set; } // ENUM con: 'Unica', 'Recurrente'

        public DateTime? FechaValidez { get; set; } // Nullable para invitaciones sin fecha específica

        [StringLength(10)] 
        public string QrCode { get; set; } // Campo para el código corto

        public int ResidenteId { get; set; } // Clave foránea al residente que lo invita

        [ForeignKey("ResidenteId")]
        public Residente Residente { get; set; } // Propiedad de navegación

        public ICollection<RegistroAcceso> RegistrosAcceso { get; set; }
    }

}
