using apiBask.Models;
using apiBask.Models.Common;
using apiBask.Models.Request;
using apiBask.Models.Response;
using apiBask.Tools;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace apiBask.Services
{
    public class UserService : IUserService
    {
        private readonly AppSettings _appSettings;        
        private readonly BasketContext _dbContext;

        public UserService(BasketContext dbContext, IOptions<AppSettings> appSettings)
        {
            _dbContext = dbContext;
            _appSettings = appSettings.Value;
        }
        public UserResponse? Auth(LoginRequest model) 
        {
            UserResponse response = new UserResponse();
            
                string spassword = Encrypt.GetSHA256(model.Password);

                var usuario = _dbContext.Usuarios.Where(d => d.Usuario1 == model.Usuario &&
                                                d.Pass == spassword).FirstOrDefault();
                if (usuario == null) return null;
                response.UserName = usuario.Usuario1;
                response.Token = GetToken(usuario);            

            return response;
        }

        private string GetToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var llave = Encoding.ASCII.GetBytes(_appSettings.Secreto);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(

                    new Claim[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, usuario.Usuario1),
                        }
                    ),
                Expires = DateTime.UtcNow.AddDays(60),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(llave),SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
