using API.Persistence.Auth.interfaces;
using MediatR;

namespace API.API_Clean_Architecture.Controllers.Auth.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse?> {
	private readonly IAuthService _AuthService;

	public RegisterCommandHandler(IAuthService authService) {
		_AuthService = authService;
	}

	public async Task<AuthResponse?> Handle(RegisterCommand request, CancellationToken cancellationToken) {
		var authData = await _AuthService.RegisterAsync(request.Email, request.Password);

		return new AuthResponse(authData.Token, authData.RefreshToken, authData.ExpiresIn);
	}
}