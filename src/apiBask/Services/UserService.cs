using apiBask.Models;
using apiBask.Models.Common;
using apiBask.Models.Request;
using apiBask.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace apiBask.Services
{
    public class UserService(BasketContext db, IOptions<AppSettings> appSettings, IPasswordHasher<Usuario> hasher) : IUserService
    {
        private readonly AppSettings _appSettings = appSettings.Value;

        // Devuelve false si el nombre de usuario ya existe.
        public async Task<bool> RegistrarAsync(RegisterRequest model)
        {
            if (await db.Usuarios.AnyAsync(u => u.Nombre == model.Usuario)) return false;

            var usuario = new Usuario { Nombre = model.Usuario };
            usuario.Pass = hasher.HashPassword(usuario, model.Pass);
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<UserResponse?> AuthAsync(LoginRequest model)
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Nombre == model.Usuario);
            if (usuario == null) return null;

            var resultado = hasher.VerifyHashedPassword(usuario, usuario.Pass, model.Password);
            if (resultado == PasswordVerificationResult.Failed) return null;
            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.Pass = hasher.HashPassword(usuario, model.Password);
                await db.SaveChangesAsync();
            }

            return new UserResponse { UserName = usuario.Nombre, Token = GetToken(usuario) };
        }

        private string GetToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var llave = Encoding.ASCII.GetBytes(_appSettings.Secreto);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Nombre),
                ]),
                Expires = DateTime.UtcNow.AddDays(60),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(llave), SecurityAlgorithms.HmacSha256Signature)
            };
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }
    }
}
