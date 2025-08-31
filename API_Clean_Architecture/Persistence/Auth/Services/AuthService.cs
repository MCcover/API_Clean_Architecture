using API.Domain.Auth;
using API.Persistence.Auth.interfaces;
using API.Utils.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Supabase;

namespace API.Persistence.Auth.Services;

[Injectable(ServiceLifetime.Scoped)]
public class AuthService : IAuthService {
	private readonly string _supabaseKey;
	private readonly string _supabaseUrl;

	public AuthService(IConfiguration configuration) {
		_supabaseUrl = configuration["Auth:URL"]!;
		_supabaseKey = configuration["Auth:Key"]!;
	}

	public async Task<AuthData?> RefreshTokenAsync(string token, string refreshToken) {
		try {
			var supabase = new Client(_supabaseUrl, _supabaseKey);
			await supabase.Auth.SetSession(token, refreshToken);

			var response = await supabase.Auth.RefreshSession();

			if (response == null || response.User == null)
				return null;

			return new AuthData {
				Token = response.AccessToken,
				RefreshToken = response.RefreshToken,
				ExpiresIn = response.ExpiresIn,
			};
		} catch (Exception ex) {
			return null;
		}
	}

	public async Task<bool> LogoutAsync(string token, string refreshToken) {
		try {
			var supabase = new Client(_supabaseUrl, _supabaseKey);
			await supabase.Auth.SetSession(token, refreshToken);
			await supabase.Auth.SignOut();
			return true;
		} catch (Exception) {
			return false;
		}
	}

	public async Task<AuthData?> LoginAsync(string email, string password) {
		try {
			var supabase = new Client(_supabaseUrl, _supabaseKey);
			var response = await supabase.Auth.SignIn(email, password);

			if (response == null || response.User == null)
				return null;

			return new AuthData {
				Token = response.AccessToken,
				RefreshToken = response.RefreshToken,
				ExpiresIn = response.ExpiresIn,
			};
		} catch {
			return null;
		}
	}

	public async Task<AuthData?> RegisterAsync(string email, string password) {
		try {
			var supabase = new Client(_supabaseUrl, _supabaseKey);
			var response = await supabase.Auth.SignUp(email, password);

			if (response?.User == null)
				return null;

			return new AuthData {
				Token = response.AccessToken ?? "",
				RefreshToken = response.RefreshToken ?? "",
				ExpiresIn = response.ExpiresIn,
			};
		} catch (Exception ex) {
			return null;
		}
	}
}