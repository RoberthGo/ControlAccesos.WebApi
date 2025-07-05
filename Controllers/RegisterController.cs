using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ControlAccesos.WebApi.Data;
using ControlAccesos.WebApi.Models;
using ControlAccesos.WebApi.DTOs;
using BCrypt.Net;
using System.Data.Common;

namespace ControlAccesos.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Guardia")]
    public class RegisterController : ControllerBase
    {
        private readonly ControlAccesosDbContext _context;
        public RegisterController(ControlAccesosDbContext context)
        {
            _context = context;
        }

        [HttpPost("registerResident")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validar el modelo (incluyendo la validación condicional de IValidatableObject)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 1. Verificar si el nombre de usuario ya existe
                if (await _context.Usuarios.AnyAsync(u => u.Username == request.Username))
                {
                    return Conflict("El nombre de usuario ya existe.");
                }
                
                // 2. Hashear la contraseña de forma segura con BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // 3. Crear la nueva entidad Usuario
                var newUser = new Usuario
                {
                    Username = request.Username,
                    ContraHash = hashedPassword,
                    Rol = request.Rol
                };

                _context.Usuarios.Add(newUser);
                await _context.SaveChangesAsync(); // Guarda el usuario para obtener su Id

                // 4. Crear la entidad Residente o Guardia y vincularla al nuevo Usuario
                if (request.Rol == "Residente")
                {
                    var newResidente = new Residente
                    {
                        Nombre = request.Nombre,
                        Apellidos = request.Apellidos,
                        Domicilio = request.Domicilio,
                        Telefono = request.Telefono,
                        Vehiculo = request.Vehiculo,
                        Placas = request.Placas,
                        UserId = newUser.Id // Vincula el residente al ID del usuario
                    };
                    _context.Residentes.Add(newResidente);
                }
                else if (request.Rol == "Guardia")
                {
                    var newGuardia = new Guardia
                    {
                        Nombre = request.Nombre,
                        Apellidos = request.Apellidos,
                        PlacasVehiculo = request.Placas, 
                        UserId = newUser.Id // Vincula el guardia al ID del usuario
                    };
                    _context.Guardias.Add(newGuardia);
                }

                await _context.SaveChangesAsync(); // Guarda el Residente/Guardia

                // Devolver una respuesta exitosa
                return StatusCode(StatusCodes.Status201Created, $"Usuario '{newUser.Username}' registrado con éxito como '{newUser.Rol}'.");
            }
            catch (DbException ex)
            {
                // Si ocurre un error en la BD después de guardar el usuario pero antes del residente/guardia
                return StatusCode(500, $"Error al registrar el usuario en la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al registrar el usuario: {ex.Message}");
            }
        }
    }
}
