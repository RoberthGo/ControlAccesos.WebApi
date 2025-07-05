using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlAccesos.WebApi.Models
{
    [Table("TokensRevocados")]
    public class TokensRevocados
    {
        [Key]
        [StringLength(128)]
        public string Jti { get; set; } // Clave primaria, el JTI del JWT

        public DateTime FechaRevocacion { get; set; }

        public DateTime? FechaExpiracionOriginal { get; set; } // Nullable
    }
}
