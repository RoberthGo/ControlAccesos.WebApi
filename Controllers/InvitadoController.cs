using ControlAccesos.WebApi.Data;
using ControlAccesos.WebApi.DTOs;
using ControlAccesos.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;

namespace ControlAccesos.WebApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class InvitadoController : ControllerBase
    {
        private readonly ControlAccesosDbContext _context;
        private static readonly Random _random = new Random(); 
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; 

        public InvitadoController(ControlAccesosDbContext context)
        {
            _context = context;
        }

        // Método auxiliar para generar un código corto único
        private async Task<string> GenerateUniqueShortCode(int length = 7)
        {
            string code;
            do
            {
                code = new string(Enumerable.Repeat(Chars, length)
                  .Select(s => s[_random.Next(s.Length)]).ToArray());
            }
            while (await _context.Invitados.AnyAsync(i => i.QrCode == code));
            return code;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Residente")] // Solo Residentes pueden crear invitados
        public async Task<IActionResult> CreateInvitado([FromBody] CreateInvitadoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Obtener el UserId del usuario autenticado desde el JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("No se pudo identificar al usuario Residente autenticado.");
            }

            // Lógica para determinar el ResidenteId del usuario autenticado
            var residente = await _context.Residentes.FirstOrDefaultAsync(r => r.UserId == userId);
            if (residente == null)
            {
                return BadRequest("El usuario autenticado no está asociado a un residente válido.");
            }

            if (request.TipoInvitacion != "Unica" && request.TipoInvitacion != "Recurrente" && request.TipoInvitacion != "PorFecha")
            {
                return BadRequest("El tipo de invitación no es válido. Los valores permitidos son 'Unica', 'Recurrente' o 'PorFecha'.");
            }


            int actualResidenteId = residente.Id;    

            try
            {
                // Generar un código de acceso único (QrCode) de 7 caracteres
                string qrCode = await GenerateUniqueShortCode(7);

                // Crear la nueva entidad Invitado
                var newInvitado = new Invitado
                {
                    Nombre = request.Nombre,
                    Apellidos = request.Apellidos,
                    TipoInvitacion = request.TipoInvitacion,
                    FechaValidez = request.FechaValidez,
                    ResidenteId = actualResidenteId, 
                    QrCode = qrCode 
                };

                _context.Invitados.Add(newInvitado);
                await _context.SaveChangesAsync(); // Guarda el invitado

                // Devolver la respuesta
                return StatusCode(StatusCodes.Status201Created, new InvitadoResponse
                {
                    Id = newInvitado.Id,
                    Nombre = newInvitado.Nombre,
                    Apellidos = newInvitado.Apellidos,
                    TipoInvitacion = newInvitado.TipoInvitacion,
                    FechaValidez = newInvitado.FechaValidez,
                    QrCode = newInvitado.QrCode, 
                    ResidenteId = newInvitado.ResidenteId
                });
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al registrar el invitado en la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al registrar el invitado: {ex.Message}");
            }
        }

        [HttpGet("validate-qr/{qrCode}")]
        [Authorize(Roles = "Guardia")] // Solo Guardias pueden validar QRs
        public async Task<IActionResult> ValidateQr(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
            {
                return BadRequest("El código QR no puede estar vacío.");
            }

            try
            {
                // Buscar el invitado por el QrCode
                var invitado = await _context.Invitados
                                             .Include(i => i.Residente) // Incluir información del residente que invita
                                             .FirstOrDefaultAsync(i => i.QrCode == qrCode);

                if (invitado == null)
                {
                    return NotFound("Código QR inválido o no encontrado.");
                }

                // Validar la fecha de validez (si aplica)
                if (invitado.FechaValidez.HasValue && invitado.FechaValidez.Value < DateTime.Now)
                {
                    return BadRequest("Permiso vencido: La fecha de validez ha expirado.");
                }

                // Validar si es de tipo "Unica" y ya ha sido usado para entrada
                var hasEntered = await _context.RegistrosAcceso
                                               .AnyAsync(ra => ra.InvitadoId == invitado.Id && ra.TipoAcceso == "Entrada");

                if (invitado.TipoInvitacion == "Unica" && hasEntered)
                {
                    return BadRequest("Este código QR ya ha sido utilizado para una entrada.");
                }

                // Si todo es válido, devolver la información del invitado
                return Ok(new
                {
                    InvitadoId = invitado.Id,
                    NombreInvitado = invitado.Nombre,
                    ApellidosInvitado = invitado.Apellidos,
                    TipoInvitacion = invitado.TipoInvitacion,
                    FechaValidez = invitado.FechaValidez,
                    ResidenteQueInvita = invitado.Residente != null ? $"{invitado.Residente.Nombre} {invitado.Residente.Apellidos}" : "N/A",
                    Mensaje = "Código QR válido. Acceso permitido."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error al validar el código QR: {ex.Message}");
            }
        }


        [HttpGet("my-invitations")]
        [Authorize(Roles = "Residente")]
        public async Task<IActionResult> GetMyInvitations()
        {
            // Obtener el UserId del usuario autenticado desde el JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("No se pudo identificar al usuario Residente autenticado.");
            }

            // Buscar el ResidenteId asociado al UserId autenticado
            var residente = await _context.Residentes.FirstOrDefaultAsync(r => r.UserId == userId);
            if (residente == null)
            {
                return BadRequest("El usuario autenticado no está asociado a un residente válido.");
            }

            try
            {
                var invitados = await _context.Invitados
                                              .Where(i => i.ResidenteId == residente.Id)
                                              .ToListAsync();

                if (!invitados.Any())
                {
                    return NotFound("No se encontraron invitaciones registradas por este residente.");
                }

                var response = invitados.Select(i => new InvitadoResponse
                {
                    Id = i.Id,
                    Nombre = i.Nombre,
                    Apellidos = i.Apellidos,
                    TipoInvitacion = i.TipoInvitacion,
                    FechaValidez = i.FechaValidez,
                    QrCode = i.QrCode,
                    ResidenteId = i.ResidenteId
                }).ToList();

                return Ok(response);
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al obtener las invitaciones de la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al obtener las invitaciones: {ex.Message}");
            }
        }

    }
}
