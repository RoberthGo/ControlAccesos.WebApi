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
    [Authorize(Roles = "Guardia")] // Solo los Guardias pueden registrar accesos
    public class AccesoController : ControllerBase
    {
        private readonly ControlAccesosDbContext _context;

        public AccesoController(ControlAccesosDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAccess([FromBody] RegisterAccessRequest request)
        {
            // Validar el modelo, incluyendo la validación personalizada para al menos un identificador
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Obtener el GuardiaId del usuario autenticado
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("No se pudo identificar al guardia autenticado.");
            }

            var guardia = await _context.Guardias.FirstOrDefaultAsync(g => g.UserId == userId);
            if (guardia == null)
            {
                return BadRequest("El usuario autenticado no está asociado a un guardia válido.");
            }
            int guardiaId = guardia.Id;

            int? residenteId = null;
            int? invitadoId = null;
            string nombrePersona = "Desconocido";
            string apellidosPersona = "";
            string rolPersona = "Desconocido";
            string identificadorUsado = ""; // Para la respuesta

            // Lógica de identificación:
            // Se asegura que solo uno de los campos de identificación sea proporcionado
            // gracias a la validación personalizada en RegisterAccessRequest.

            if (!string.IsNullOrWhiteSpace(request.InvitadoQrCode))
            {
                // 1. Intentar identificar como Invitado por QrCode
                var invitado = await _context.Invitados
                                             .Include(i => i.Residente) // Incluir información del residente que invita
                                             .FirstOrDefaultAsync(i => i.QrCode == request.InvitadoQrCode);

                if (invitado != null)
                {
                    invitadoId = invitado.Id;
                    nombrePersona = invitado.Nombre;
                    apellidosPersona = invitado.Apellidos;
                    rolPersona = "Invitado";
                    identificadorUsado = request.InvitadoQrCode;

                    // Validar la fecha de validez del invitado
                    if (invitado.FechaValidez.HasValue && invitado.FechaValidez.Value < DateTime.Now)
                    {
                        return BadRequest("Acceso denegado: El permiso del invitado ha expirado.");
                    }

                    // Validar tipo de invitación "Unica"
                    var hasEntered = await _context.RegistrosAcceso
                                                   .AnyAsync(ra => ra.InvitadoId == invitado.Id && ra.TipoAcceso == "Entrada");

                    if (invitado.TipoInvitacion == "Unica" && hasEntered)
                    {
                        return BadRequest("Acceso denegado: Este código de invitado único ya ha sido utilizado para una entrada.");
                    }
                }
                else
                {
                    return NotFound("Código QR de invitado inválido o no encontrado.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.ResidentUsername))
            {
                // 2. Intentar identificar como Residente por Username
                var userResidente = await _context.Usuarios
                                                  .Include(u => u.Residentes) // Incluir la colección de Residentes
                                                  .FirstOrDefaultAsync(u => u.Username == request.ResidentUsername && u.Rol == "Residente");

                if (userResidente != null && userResidente.Residentes.Any())
                {
                    var residente = userResidente.Residentes.FirstOrDefault(); // Tomar el primer residente asociado
                    residenteId = residente.Id;
                    nombrePersona = residente.Nombre;
                    apellidosPersona = residente.Apellidos;
                    rolPersona = "Residente";
                    identificadorUsado = request.ResidentUsername;
                }
                else
                {
                    return NotFound("Nombre de usuario de residente inválido o no encontrado.");
                }
            }

            try
            {
                // Crear el registro de acceso
                var newRegistro = new RegistroAcceso
                {
                    FechaHora = DateTime.Now,
                    TipoAcceso = request.TipoAcceso,
                    ResidenteId = residenteId,
                    InvitadoId = invitadoId,
                    GuardiaId = guardiaId,
                    PlacasVehiculo = request.PlacasVehiculo, // Las placas del vehículo que está entrando/saliendo
                    Notas = request.Notas
                };

                _context.RegistrosAcceso.Add(newRegistro);
                await _context.SaveChangesAsync();

                // Devolver la respuesta exitosa
                return StatusCode(StatusCodes.Status201Created, new AccessResponse
                {
                    RegistroId = newRegistro.Id,
                    FechaHora = newRegistro.FechaHora,
                    TipoAcceso = newRegistro.TipoAcceso,
                    IdentificadorAcceso = identificadorUsado, // Ahora refleja el identificador que realmente se usó
                    NombrePersona = nombrePersona,
                    ApellidosPersona = apellidosPersona,
                    RolPersona = rolPersona,
                    NombreGuardia = $"{guardia.Nombre} {guardia.Apellidos}",
                    PlacasVehiculo = newRegistro.PlacasVehiculo,
                    Mensaje = $"Acceso de {rolPersona} '{nombrePersona} {apellidosPersona}' registrado como '{newRegistro.TipoAcceso}' con éxito."
                });
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al registrar el acceso en la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al registrar el acceso: {ex.Message}");
            }
        }

        // Método para obtener todos los registros de acceso
        [HttpGet("history")]
        public async Task<IActionResult> GetAccessHistory([FromQuery] AccessHistoryRequest request)
        {
            try
            {
                IQueryable<RegistroAcceso> query = _context.RegistrosAcceso
                    .Include(ra => ra.Residente)
                    .Include(ra => ra.Invitado)
                        .ThenInclude(i => i.Residente) // Incluir el residente que invitó al invitado
                    .Include(ra => ra.Guardia);

                // Aplicar filtros
                if (request.FechaInicio.HasValue)
                {
                    query = query.Where(ra => ra.FechaHora >= request.FechaInicio.Value);
                }
                if (request.FechaFin.HasValue)
                {
                    query = query.Where(ra => ra.FechaHora <= request.FechaFin.Value);
                }
                if (request.ResidenteId.HasValue)
                {
                    query = query.Where(ra => ra.ResidenteId == request.ResidenteId.Value);
                }
                if (request.InvitadoId.HasValue)
                {
                    query = query.Where(ra => ra.InvitadoId == request.InvitadoId.Value);
                }
                if (!string.IsNullOrWhiteSpace(request.TipoAcceso))
                {
                    query = query.Where(ra => ra.TipoAcceso == request.TipoAcceso);
                }
                if (request.GuardiaId.HasValue)
                {
                    query = query.Where(ra => ra.GuardiaId == request.GuardiaId.Value);
                }
                if (!string.IsNullOrWhiteSpace(request.PlacasVehiculo))
                {
                    query = query.Where(ra => ra.PlacasVehiculo == request.PlacasVehiculo);
                }

                // Ordenar por fecha y hora descendente por defecto
                query = query.OrderByDescending(ra => ra.FechaHora);

                var records = await query.ToListAsync();

                if (!records.Any())
                {
                    return NotFound("No se encontraron registros de acceso con los filtros especificados.");
                }

                // Mapear a DTOs de respuesta
                var accessHistory = records.Select(ra => new AccessRecordDto
                {
                    Id = ra.Id,
                    FechaHora = ra.FechaHora,
                    TipoAcceso = ra.TipoAcceso,
                    NombreResidente = ra.Residente?.Nombre,
                    ApellidosResidente = ra.Residente?.Apellidos,
                    DomicilioResidente = ra.Residente?.Domicilio, // Añadir más campos si son útiles
                    NombreInvitado = ra.Invitado?.Nombre,
                    ApellidosInvitado = ra.Invitado?.Apellidos,
                    TipoInvitacionInvitado = ra.Invitado?.TipoInvitacion, // Añadir más campos si son útiles
                    NombreGuardia = ra.Guardia?.Nombre,
                    ApellidosGuardia = ra.Guardia?.Apellidos,
                    PlacasVehiculo = ra.PlacasVehiculo,
                    Notas = ra.Notas
                }).ToList();

                return Ok(accessHistory);
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al consultar el historial de acceso en la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al obtener el historial de acceso: {ex.Message}");
            }
        }
    }




}


