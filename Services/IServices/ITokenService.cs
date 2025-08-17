using TCBackend.Model.LoginSecurity;

namespace TCBackend.Services.IServices
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
