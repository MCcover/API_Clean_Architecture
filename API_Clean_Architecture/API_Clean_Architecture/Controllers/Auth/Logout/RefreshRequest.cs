namespace API.API_Clean_Architecture.Controllers.Auth.Refresh;

public class LogoutRequest {
	public string RefreshToken { get; set; } = string.Empty;
}