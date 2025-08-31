using API.Persistence.Auth.interfaces;
using MediatR;

namespace API.API_Clean_Architecture.Controllers.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse?> {
	private readonly IAuthService _AuthService;

	public LoginCommandHandler(IAuthService authService) {
		_AuthService = authService;
	}

	public async Task<AuthResponse?> Handle(LoginCommand request, CancellationToken cancellationToken) {
		var authData = await _AuthService.LoginAsync(request.Email, request.Password);
		return new AuthResponse(authData.Token, authData.RefreshToken, authData.ExpiresIn);
	}
}