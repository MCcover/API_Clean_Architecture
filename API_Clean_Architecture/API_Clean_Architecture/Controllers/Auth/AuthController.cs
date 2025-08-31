using System.Diagnostics;
using API.API_Clean_Architecture.Controllers.Auth.Login;
using API.API_Clean_Architecture.Controllers.Auth.Logout;
using API.API_Clean_Architecture.Controllers.Auth.Refresh;
using API.API_Clean_Architecture.Controllers.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.API_Clean_Architecture.Controllers.Auth;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase {
	private readonly IMediator _Mediator;

	public AuthController(IMediator mediator) {
		_Mediator = mediator;
	}

	[HttpPost("login")]
	public async Task Login([FromBody] LoginRequest request) {
		var result = await _Mediator.Send(new LoginCommand(request.Email, request.Password));

		AddCookies(result);
	}

	[HttpPost("register")]
	public async Task Register([FromBody] RegisterRequest request) {
		var result = await _Mediator.Send(new RegisterCommand(request.Email, request.Password));
		AddCookies(result);
	}

	[HttpPost("refresh")]
	public async Task RefreshToken() {
		var currentToken = Request.Cookies["authToken"];
		var refreshToken = Request.Cookies["refreshToken"];

		var result = await _Mediator.Send(new RefreshTokenCommand(currentToken, refreshToken));

		DeleteCookies();
		AddCookies(result);
	}

	[HttpPost("logout")]
	[Authorize]
	public async Task Logout() {
		var currentToken = Request.Cookies["authToken"];
		var refreshToken = Request.Cookies["refreshToken"];

		await _Mediator.Send(new LogoutCommand(currentToken, refreshToken));

		DeleteCookies();
	}

	private void DeleteCookies() {
		Response.Cookies.Delete("authToken");
		Response.Cookies.Delete("refreshToken");
	}

	private void AddCookies(AuthResponse result) {
		var isDebug = Debugger.IsAttached;
		var cookieOptions = new CookieOptions {
			HttpOnly = true, // Previene acceso desde JavaScript (XSS protection)
			Secure = !isDebug, // Solo HTTPS en producción
			SameSite = SameSiteMode.Strict, // Protección CSRF
			Expires = DateTimeOffset.UtcNow.AddSeconds(result.ExpiresIn),
		};

		var refreshCookieOptions = new CookieOptions {
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.Strict,
			Expires = DateTimeOffset.UtcNow.AddDays(30),
		};

		Response.Cookies.Append("authToken", result.Token, cookieOptions);
		Response.Cookies.Append("refreshToken", result.RefreshToken, refreshCookieOptions);
	}
}