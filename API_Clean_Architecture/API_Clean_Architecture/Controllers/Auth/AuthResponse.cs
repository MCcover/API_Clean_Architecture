namespace API.API_Clean_Architecture.Controllers.Auth;

public class AuthResponse {
	public string Token { get; set; }
	public string RefreshToken { get; set; }
	public long ExpiresIn { get; set; }

	public AuthResponse(string token, string refreshToken, long expiresIn) {
		Token = token;
		RefreshToken = refreshToken;
		ExpiresIn = expiresIn;
	}
}