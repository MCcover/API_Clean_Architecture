﻿using API.Persistence.Auth.interfaces;
using MediatR;

namespace API.API_Clean_Architecture.Controllers.Auth.Refresh;

public class RefreshCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto?> {
	private readonly IAuthService _AuthService;

	public RefreshCommandHandler(IAuthService authService) {
		_AuthService = authService;
	}

	public async Task<AuthResponseDto?> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) {
		var authData = await _AuthService.RefreshTokenAsync(request.Token, request.Refresh);
		return new AuthResponseDto(authData.Token, authData.RefreshToken, authData.ExpiresIn);
	}
}