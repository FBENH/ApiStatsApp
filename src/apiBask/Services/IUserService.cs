using apiBask.Models.Request;
using apiBask.Models.Response;

namespace apiBask.Services
{
    public interface IUserService
    {
        Task<bool> RegistrarAsync(RegisterRequest model);

        Task<UserResponse?> AuthAsync(LoginRequest model);
    }
}
