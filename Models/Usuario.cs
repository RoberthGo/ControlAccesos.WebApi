using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlAccesos.WebApi.Models
{
    [Table("Usuarios")] // Asegura que EF Core mapee a la tabla 'Usuarios'
    public class Usuario
    {
        [Key] // Marca 'Id' como clave primaria
        public int Id { get; set; }

        [Column("Usuario")] // Mapea a la columna 'Usuario' en la BD
        [StringLength(50)]
        public string Username { get; set; } 

        [StringLength(256)]
        public string ContraHash { get; set; }

        [StringLength(20)]
        public string Rol { get; set; } // ENUM con: 'Residente', 'Guardia'
        public ICollection<Residente> Residentes { get; set; }
        public ICollection<Guardia> Guardias { get; set; }
    }
}
