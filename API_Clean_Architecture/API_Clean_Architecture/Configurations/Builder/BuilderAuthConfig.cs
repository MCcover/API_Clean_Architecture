using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace API.API_Clean_Architecture.Configurations.Builder;

public static class BuilderAuthConfig {
    public static void ConfigureAuth(this IHostApplicationBuilder builder) {
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
                    if (!string.IsNullOrEmpty(token)) {
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                },
            };
        });
        builder.Services.AddAuthorization();
    }

}