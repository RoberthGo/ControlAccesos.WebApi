using BCrypt.Net;
using ControlAccesos.WebApi.Data;
using ControlAccesos.WebApi.DTOs;
using ControlAccesos.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace ControlAccesos.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ControlAccesosDbContext _context;

        public AccountController(IConfiguration configuration, ControlAccesosDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }



        [Authorize(Roles = "Guardia")]
        [HttpPost("registerUser")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
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
                    var problemDetails = new ProblemDetails
                    {
                        Type = "https://tuapi.com/errors/usuario-existente", // URI para este tipo de error
                        Title = "Conflicto de Recurso",
                        Status = (int)HttpStatusCode.Conflict, // 409
                        Instance = HttpContext.Request.Path //  URI específico del recurso donde ocurrió el error
                    };
                    problemDetails.Extensions["errors"] = new Dictionary<string, string[]>
                    {
                        { "UserName", new string[] { "El nombre de usuario ya existe." } }
                    };
                    return Conflict(problemDetails);
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
                var problemDetails = new ProblemDetails
                {
                    Type = "https://tuapi.com/errors/database-error",
                    Title = "Error de Base de Datos",
                    Detail = $"Ocurrió un error al crear el residente/guardia: {ex.Message}",
                    Status = (int)HttpStatusCode.InternalServerError, // 500
                    Instance = HttpContext.Request.Path
                };

                // Puedes añadir información adicional si es útil para el cliente
                problemDetails.Extensions["codigoErrorInterno"] = ex.ErrorCode; // Si el DbException lo expone
                problemDetails.Extensions["tipoExcepcion"] = nameof(DbException);

                // Devuelve el JSON de ProblemDetails con el código 500
                return StatusCode((int)HttpStatusCode.InternalServerError, problemDetails);
            }
            catch (Exception ex)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "https://tuapi.com/errors/unhandled-exception",
                    Title = "Error Interno del Servidor",
                    Detail = "Ocurrió un error inesperado. Por favor, inténtelo de nuevo más tarde.",
                    Status = (int)HttpStatusCode.InternalServerError, // 500
                    Instance = HttpContext.Request.Path
                };

                problemDetails.Extensions["mensajeTecnico"] = ex.Message;
                problemDetails.Extensions["stackTrace"] = ex.StackTrace;
                problemDetails.Extensions["tipoExcepcion"] = nameof(Exception);

                // Devuelve el JSON de ProblemDetails con el código 500
                return StatusCode((int)HttpStatusCode.InternalServerError, problemDetails);
            }
        }





        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Validar Credenciales del Usuario contra la Base de Datos
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return Unauthorized("Credenciales inválidas: Nombre de usuario no encontrado.");
            }

            // Compara la contraseña proporcionada con el hash almacenado usando BCrypt.Verify
            // BCrypt.Verify ya maneja el salt internamente al comparar.
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.ContraHash))
            {
                return Unauthorized("Credenciales inválidas");
            }

            // Paso 2: Generar el JWT
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),    // ID del usuario (sub)
                new Claim(ClaimTypes.Name, user.Username),                  // Nombre de usuario
                new Claim(ClaimTypes.Role, user.Rol),                       // Rol del usuario (Residente, Guardia)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ID único del JWT para revocación
            };

            int? residenteId = null;
            if (user.Rol == "Residente")
            {
                var residente = await _context.Residentes.FirstOrDefaultAsync(r => r.UserId == user.Id);
                if (residente != null)
                {
                    residenteId = residente.Id;
                }
            }

            var jwtKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                expires: DateTime.Now.AddHours(3), // El token expira en 3 horas 
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            // Paso 3: Devolver el JWT al Cliente
            return Ok(new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = user.Username,
                Rol = user.Rol,
                ResidenteId =  residenteId
            });
        }
    }
}
