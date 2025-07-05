using ControlAccesos.WebApi.Data; 
using ControlAccesos.WebApi.DTOs; 
using ControlAccesos.WebApi.Models; 
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;


namespace ControlAccesos.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ControlAccesosDbContext _context;

        public AuthController(IConfiguration configuration, ControlAccesosDbContext context)
        {
            _configuration = configuration;
            _context = context;
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
                Rol = user.Rol
            });
        }
    }
}
