using apiBask.Models.Request;
using apiBask.Models.Response;

namespace apiBask.Services
{
    public interface IUserService
    {
        public UserResponse Auth(LoginRequest model);
    }
}
