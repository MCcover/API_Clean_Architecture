using API.Persistence.Auth.interfaces;
using MediatR;

namespace API.API_Clean_Architecture.Controllers.Auth.Refresh;

public class RefreshCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse?> {
	private readonly IAuthService _AuthService;

	public RefreshCommandHandler(IAuthService authService) {
		_AuthService = authService;
	}

	public async Task<AuthResponse?> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) {
		var authData = await _AuthService.RefreshTokenAsync(request.Token, request.Refresh);
		return new AuthResponse(authData.Token, authData.RefreshToken, authData.ExpiresIn);
	}
}