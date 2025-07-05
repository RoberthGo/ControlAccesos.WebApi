using Microsoft.EntityFrameworkCore;
using ControlAccesos.WebApi.Models;

namespace ControlAccesos.WebApi.Data
{
    public class ControlAccesosDbContext : DbContext
    {
        // Constructor que acepta DbContextOptions para la configuración
        public ControlAccesosDbContext(DbContextOptions<ControlAccesosDbContext> options)
            : base(options)
        {
        }

        // DbSet para cada una de las entidades (tablas en la base de datos)
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Residente> Residentes { get; set; }
        public DbSet<Guardia> Guardias { get; set; }
        public DbSet<Invitado> Invitados { get; set; }
        public DbSet<RegistroAcceso> RegistrosAcceso { get; set; }
        

        // Se configura el mapeo de los modelos a la BD
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de ENUMs por si EF Core tiene problemas para mapearlos directamente
            modelBuilder.Entity<Usuario>()
                .Property(u => u.Rol)
                .HasConversion<string>(); // Mapea el ENUM 'Rol' a string

            modelBuilder.Entity<Invitado>()
                .Property(i => i.TipoInvitacion)
                .HasConversion<string>(); // Mapea el ENUM 'TipoInvitacion' a string

            modelBuilder.Entity<RegistroAcceso>()
                .Property(ra => ra.TipoAcceso)
                .HasConversion<string>(); // Mapea el ENUM 'TipoAcceso' a string

            // Configuración de índices únicos si no se infieren automáticamente
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<Invitado>() // Índice único para QrCode
                .HasIndex(i => i.QrCode)
                .IsUnique();
        }

    }
}
