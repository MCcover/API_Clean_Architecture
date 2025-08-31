using System.Diagnostics;
using System.Reflection;
using System.Text;
using API.Utils.DI;
using API.Utils.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace API.API_Clean_Architecture.Configurations.Builder;

public static class BuilderConfigure {
	public static WebApplicationBuilder Configure(this WebApplicationBuilder builder) {
		builder.ConfigureCors();
		builder.ConfigureJson();
		builder.ConfigureSwagger();
		builder.ConfigureResponseCompression();
		builder.ConfigureRateLimiter();

		builder.Services.AddInjectables();
		builder.Services.AddMediatR(cnf => {
			var sad = AppDomain.CurrentDomain.GetProjectAssemblies();
			if (sad == null) {
				return;
			}

			var assemblies = sad.Select(Assembly.LoadFrom).ToArray();
			cnf.RegisterServicesFromAssemblies(assemblies);
		});

		builder.ConfigureLogging();

		var secret = builder.Configuration["Auth:Secret"] ?? throw new Exception("Missing Secret");
		var secretBytes = Encoding.UTF8.GetBytes(secret);
		builder.Services.AddAuthentication(options => {
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		}).AddJwtBearer(opt => {
			if (!Debugger.IsAttached) {
				opt.RequireHttpsMetadata = true;
			}

			opt.SaveToken = true;
			opt.TokenValidationParameters = new TokenValidationParameters {
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(secretBytes),

				ValidateIssuer = true,
				ValidIssuer = builder.Configuration["Auth:ValidIssuer"],

				ValidateAudience = true,
				ValidAudience = builder.Configuration["Auth:ValidAudience"],

				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero,
			};
			opt.Events = new JwtBearerEvents {
				OnMessageReceived = context => {
					var token = context.Request.Cookies["authToken"];

					// Asignar el token al contexto
					if (!string.IsNullOrEmpty(token)) {
						context.Token = token;
					}

					return Task.CompletedTask;
				},
			};
		});
		builder.Services.AddAuthorization();

		builder.ConfigureControllers();
		return builder;
	}
}