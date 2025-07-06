using ControlAccesos.WebApi.Data;
using ControlAccesos.WebApi.DTOs;
using ControlAccesos.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace ControlAccesos.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Guardia")]
    public class ResidentesController : ControllerBase
    {
        private readonly ControlAccesosDbContext _context;
        public ResidentesController(ControlAccesosDbContext context)
        {
            _context = context;
        }

        // GET: api/Residentes/getAllBy
        [HttpGet("getAllBy")]
        public async Task<IActionResult> GetAllBy( 
            [FromQuery] string? nombre = null,
            [FromQuery] string? apellidos = null,
            [FromQuery] string? domicilio = null)
        {
            try
            {
                IQueryable<Residente> query = _context.Residentes
                                                        .Include(r => r.Usuario); 

                // Aplicar filtros si se proporcionan
                if (!string.IsNullOrWhiteSpace(nombre))
                {
                    query = query.Where(r => r.Nombre.Contains(nombre));
                }
                if (!string.IsNullOrWhiteSpace(apellidos))
                {
                    query = query.Where(r => r.Apellidos.Contains(apellidos));
                }
                if (!string.IsNullOrWhiteSpace(domicilio))
                {
                    query = query.Where(r => r.Domicilio != null && r.Domicilio.Contains(domicilio));
                }

                var residentes = await query.ToListAsync();

                if (!residentes.Any())
                {
                    return NotFound("No se encontraron residentes con los filtros especificados.");
                }

                var response = residentes.Select(r => new ResidentResponse
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Apellidos = r.Apellidos,
                    Domicilio = r.Domicilio,
                    Telefono = r.Telefono,
                    Vehiculo = r.Vehiculo,
                    Placas = r.Placas,
                    UserId = r.UserId,
                    Username = r.Usuario?.Username
                }).ToList();

                return Ok(response);
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al obtener residentes de la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al obtener los residentes: {ex.Message}");
            }
        }


        // GET: api/Residentes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetResidentById(int id)
        {
            try
            {
                var residente = await _context.Residentes
                                                .Include(r => r.Usuario) // Incluir el usuario asociado
                                                .FirstOrDefaultAsync(r => r.Id == id);

                if (residente == null)
                {
                    return NotFound($"Residente con ID {id} no encontrado.");
                }

                var response = new ResidentResponse
                {
                    Id = residente.Id,
                    Nombre = residente.Nombre,
                    Apellidos = residente.Apellidos,
                    Domicilio = residente.Domicilio,
                    Telefono = residente.Telefono,
                    Vehiculo = residente.Vehiculo,
                    Placas = residente.Placas,
                    UserId = residente.UserId,
                    Username = residente.Usuario?.Username
                };

                return Ok(response);
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al obtener el residente de la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al obtener el residente: {ex.Message}");
            }
        }



        // PUT: api/Residentes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateResident(int id, [FromBody] ResidentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var residenteToUpdate = await _context.Residentes.FindAsync(id);
            if (residenteToUpdate == null)
            {
                return NotFound($"Residente con ID {id} no encontrado.");
            }

            
            try
            {
                residenteToUpdate.Nombre = request.Nombre;
                residenteToUpdate.Apellidos = request.Apellidos;
                residenteToUpdate.Domicilio = request.Domicilio;
                residenteToUpdate.Telefono = request.Telefono;
                residenteToUpdate.Vehiculo = request.Vehiculo;
                residenteToUpdate.Placas = request.Placas;

                _context.Entry(residenteToUpdate).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                string? username = (await _context.Usuarios.FindAsync(residenteToUpdate.UserId))?.Username;

                return Ok(new ResidentResponse
                {
                    Id = residenteToUpdate.Id,
                    Nombre = residenteToUpdate.Nombre,
                    Apellidos = residenteToUpdate.Apellidos,
                    Domicilio = residenteToUpdate.Domicilio,
                    Telefono = residenteToUpdate.Telefono,
                    Vehiculo = residenteToUpdate.Vehiculo,
                    Placas = residenteToUpdate.Placas,
                    UserId = residenteToUpdate.UserId, 
                    Username = username
                });
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al actualizar el residente en la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al actualizar el residente: {ex.Message}");
            }
        }


        // PUT: Delete/Residentes/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Guardia")]
        public async Task<IActionResult> DeleteResident(int id)
        {
            var residenteToDelete = await _context.Residentes.FindAsync(id);
            if (residenteToDelete == null)
            {
                return NotFound($"Residente con ID {id} no encontrado.");
            }

            try
            {
                // Manejo de dependencias antes de eliminar un residente:
                var invitadosAsociados = await _context.Invitados.Where(i => i.ResidenteId == id).ToListAsync();
                _context.Invitados.RemoveRange(invitadosAsociados);

                // 2. RegistrosAcceso que referencian a este residente:
                var registrosAsociados = await _context.RegistrosAcceso.Where(ra => ra.ResidenteId == id).ToListAsync();
                foreach (var registro in registrosAsociados)
                {
                    registro.ResidenteId = null; // Desvincular en lugar de eliminar
                }
                _context.RegistrosAcceso.UpdateRange(registrosAsociados);

                _context.Residentes.Remove(residenteToDelete);
                await _context.SaveChangesAsync();

                return Ok($"Residente con ID {id} eliminado con éxito.");
            }
            catch (DbException ex)
            {
                return StatusCode(500, $"Error al eliminar el residente de la base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error inesperado al eliminar el residente: {ex.Message}");
            }
        }

    }
}
