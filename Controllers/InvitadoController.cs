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
                    ResidenteId = newInvitado.ResidenteId,
                    EstadoQr = "Activo"
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

                DateTime canceladoSentinel = DateTime.MinValue; // 0001-01-01T00:00:00
                // Verificar si está cancelado
                if (invitado.FechaValidez.HasValue && invitado.FechaValidez.Value == canceladoSentinel)
                {
                    return BadRequest("Permiso cancelado: Este código QR ha sido anulado.");
                }
                
                // Validar la fecha de validez (si aplica)
                if (invitado.FechaValidez.HasValue && invitado.FechaValidez.Value < DateTime.Now)
                {
                    return BadRequest("Permiso vencido: La fecha de validez ha expirado.");
                }

                // Validar si es de tipo "Unica" y ya ha sido usado para entrada

                if (invitado.TipoInvitacion == "Unica" && invitado.RegistrosAcceso.Any() && invitado.RegistrosAcceso.Any(ra => ra.TipoAcceso == "Salida"))
                {
                    return BadRequest("Este código QR ya ha sido utilizado.");
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
                                              .Include(i => i.RegistrosAcceso)
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
                    ResidenteId = i.ResidenteId,
                    EstadoQr = GetQrStatus(i)
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


        [HttpPut("{id}")] 
        [Authorize(Roles = "Residente")] // Solo el residente que la creó puede actualizarla
        public async Task<IActionResult> UpdateInvitado(int id, [FromBody] UpdateInvitadoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Obtener el UserId del residente autenticado
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

            // Buscar la invitación por ID y asegurarse de que pertenezca a este residente
            var invitadoToUpdate = await _context.Invitados
                                                 .Include(i => i.RegistrosAcceso) // Incluir registros para validación
                                                 .FirstOrDefaultAsync(i => i.Id == id && i.ResidenteId == residente.Id);
            if (invitadoToUpdate == null)
            {
                return NotFound("Invitación no encontrada o no pertenece a este residente.");
            }

            // Validaciones para no actualizar si ya está en un estado final o no modificable
            DateTime canceladoSentinel = DateTime.MinValue;

            if (invitadoToUpdate.FechaValidez.HasValue && invitadoToUpdate.FechaValidez.Value == canceladoSentinel)
            {
                return BadRequest("No se puede actualizar una invitación que ya está cancelada.");
            }
            if (invitadoToUpdate.FechaValidez.HasValue && invitadoToUpdate.FechaValidez.Value < DateTime.Now)
            {
                return BadRequest("No se puede actualizar una invitación que ya ha vencido.");
            }

            try
            {
                // Aplicar solo los campos que se proporcionan en el request
                if (!string.IsNullOrWhiteSpace(request.Nombre))
                {
                    invitadoToUpdate.Nombre = request.Nombre;
                }
                if (!string.IsNullOrWhiteSpace(request.Apellidos))
                {
                    invitadoToUpdate.Apellidos = request.Apellidos;
                }
                if (!string.IsNullOrWhiteSpace(request.TipoInvitacion))
                {
                    // Opcional: Validar que TipoInvitacion sea un ENUM válido ("Unica", "Recurrente", "PorFecha")
                    if (request.TipoInvitacion != "Unica" && request.TipoInvitacion != "Recurrente" && request.TipoInvitacion != "PorFecha")
                    {
                        return BadRequest("Tipo de invitación inválido. Valores permitidos: 'Unica', 'Recurrente', 'PorFecha'.");
                    }
                    invitadoToUpdate.TipoInvitacion = request.TipoInvitacion;
                }
                if (request.FechaValidez.HasValue)
                {
                    invitadoToUpdate.FechaValidez = request.FechaValidez;
                }

                _context.Entry(invitadoToUpdate).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Devolver la invitación actualizada
                return Ok(new InvitadoResponse
                {
                    Id = invitadoToUpdate.Id,
                    Nombre = invitadoToUpdate.Nombre,
                    Apellidos = invitadoToUpdate.Apellidos,
                    TipoInvitacion = invitadoToUpdate.TipoInvitacion,
                    FechaValidez = invitadoToUpdate.FechaValidez,
                    QrCode = invitadoToUpdate.QrCode,
                    ResidenteId = invitadoToUpdate.ResidenteId,
                    EstadoQr = GetQrStatus(invitadoToUpdate)       
                }); 
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al actualizar la invitación en la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al actualizar la invitación: {ex.Message}");
            }
        }


        [HttpPut("cancel/{id}")] 
        [Authorize(Roles = "Residente")] // Solo el residente que la creó puede cancelarla
        public async Task<IActionResult> CancelInvitation(int id)
        {
            // Obtener el UserId del residente autenticado
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

            // Buscar la invitación por ID y asegurarse de que pertenezca a este residente
            var invitadoToCancel = await _context.Invitados.FirstOrDefaultAsync(i => i.Id == id && i.ResidenteId == residente.Id);
            if (invitadoToCancel == null)
            {
                return NotFound("Invitación no encontrada o no pertenece a este residente.");
            }

            DateTime canceladoSentinel = DateTime.MinValue;

            // Validaciones para no cancelar si ya está en un estado final
            if (invitadoToCancel.FechaValidez.HasValue && invitadoToCancel.FechaValidez.Value == canceladoSentinel)
            {
                return BadRequest("La invitación ya está cancelada.");
            }
            if (invitadoToCancel.FechaValidez.HasValue && invitadoToCancel.FechaValidez.Value < DateTime.Now)
            {
                return BadRequest("La invitación ya está vencida y no puede ser cancelada.");
            }

            // Cargar RegistrosAcceso para la validación de "Usado"
            await _context.Entry(invitadoToCancel).Collection(i => i.RegistrosAcceso).LoadAsync();
            if (invitadoToCancel.TipoInvitacion == "Unica" && invitadoToCancel.RegistrosAcceso != null && invitadoToCancel.RegistrosAcceso.Any(ra => ra.TipoAcceso == "Entrada"))
            {
                return BadRequest("La invitación ya ha sido usada y no puede ser cancelada.");
            }

            try
            {
                invitadoToCancel.FechaValidez = canceladoSentinel;
                _context.Entry(invitadoToCancel).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok("Invitación cancelada con éxito.");
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al cancelar la invitación en la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al cancelar la invitación: {ex.Message}");
            }
        }



        [HttpDelete("{id}")] 
        [Authorize(Roles = "Residente")] // Solo el residente que la creó puede eliminarla
        public async Task<IActionResult> DeleteInvitation(int id) 
        {
            // Obtener el UserId del residente autenticado
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

            // Buscar la invitación por ID y asegurarse de que pertenezca a este residente
            var invitadoToDelete = await _context.Invitados.FirstOrDefaultAsync(i => i.Id == id && i.ResidenteId == residente.Id);
            if (invitadoToDelete == null)
            {
                return NotFound("Invitación no encontrada o no pertenece a este residente.");
            }

            
            // No se podra eliminar la invitacion si ya fue usada o vencida
            DateTime canceladoSentinel = DateTime.MinValue;

            if (invitadoToDelete.FechaValidez.HasValue && invitadoToDelete.FechaValidez.Value < DateTime.Now && invitadoToDelete.FechaValidez.Value != canceladoSentinel)
            {
                return BadRequest("No se puede eliminar una invitación que ya ha vencido.");
            }

            if (invitadoToDelete.RegistrosAcceso.Any())
            {
                return BadRequest("No se puede eliminar una invitación que ya ha sido usada o ha generado registros de acceso.");
            }

            try
            {
                _context.Invitados.Remove(invitadoToDelete);
                await _context.SaveChangesAsync(); 

                return Ok($"Invitación con ID {id} eliminada con éxito.");
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al eliminar la invitación en la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al eliminar la invitación: {ex.Message}");
            }
        }




        private string GetQrStatus(Invitado invitado)
        {

            DateTime canceladoSentinel = DateTime.MinValue;

            // 1. Verificar si está cancelado
            if (invitado.FechaValidez.HasValue && invitado.FechaValidez.Value == canceladoSentinel)
            {
                return "Cancelado";
            }

            // 2. Verificar si está vencido
            if (invitado.FechaValidez.HasValue && invitado.FechaValidez.Value < DateTime.Now)
            {
                return "Vencido";
            }

            // 3. Verificar si ya fue usado (solo para tipo "Unica")
            if (invitado.TipoInvitacion == "Unica" &&  invitado.RegistrosAcceso.Any())
            {
                return "Usado";
            }
            
            return "Activo";
        }
    }
}
