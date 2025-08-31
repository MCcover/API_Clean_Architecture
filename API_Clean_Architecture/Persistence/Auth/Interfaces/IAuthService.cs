using API.Domain.Auth;

namespace API.Persistence.Auth.interfaces;

public interface IAuthService {
	Task<AuthData?> LoginAsync(string email, string password);
	Task<AuthData?> RegisterAsync(string email, string password);
	Task<AuthData?> RefreshTokenAsync(string token, string refreshToken);
	Task<bool> LogoutAsync(string token, string refreshToken);
}