using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlAccesos.WebApi.Models
{
    [Table("RegistrosAcceso")]
    public class RegistroAcceso
    {
        [Key]
        public int Id { get; set; }

        public DateTime FechaHora { get; set; }

        [StringLength(20)]
        public string TipoAcceso { get; set; } // Corresponde a ENUM en MySQL: 'Entrada', 'Salida'
        public int? ResidenteId { get; set; } // Nullable
        public int? InvitadoId { get; set; } // Nullable
        public int GuardiaId { get; set; } 

        [StringLength(20)]
        public string PlacasVehiculo { get; set; }

        [StringLength(500)]
        public string Notas { get; set; }

        [ForeignKey("ResidenteId")]
        public Residente Residente { get; set; }

        [ForeignKey("InvitadoId")]
        public Invitado Invitado { get; set; }

        [ForeignKey("GuardiaId")]
        public Guardia Guardia { get; set; }
    }

}
