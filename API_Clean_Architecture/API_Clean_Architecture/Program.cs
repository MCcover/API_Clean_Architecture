using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using API.API_Clean_Architecture.Filters;
using API.CompositionRoot;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

const string MY_CORS = "MY CORS";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => {
	options.AddPolicy(MY_CORS,
		corsPolicyBuilder => {
			corsPolicyBuilder.AllowAnyOrigin()
				.AllowAnyHeader()
				.AllowAnyMethod();
		});
});

builder.Services.AddSwaggerGen(c => {
	c.SwaggerDoc("v1", new OpenApiInfo {
		Title = "API", Version = "v1",
	});

	c.OperationFilter<FileUploadOperationFilter>();

	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
		In = ParameterLocation.Header,
		Description = "Please insert JWT with Bearer into field",
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey,
	});

	c.AddSecurityRequirement(new OpenApiSecurityRequirement {
		{
			new OpenApiSecurityScheme {
				Reference = new OpenApiReference {
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer",
				},
			},
			Array.Empty<string>()
		},
	});
});

builder.Services.AddControllers().AddJsonOptions(options => {
	options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddServices(builder.Configuration.GetConnectionString("Connection"));

builder.Services.ConfigureHttpJsonOptions(options => {
	options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
	options.SerializerOptions.WriteIndented = false;
});

builder.Services.AddResponseCompression(options => {
	options.EnableForHttps = true;
	options.Providers.Add<GzipCompressionProvider>();
	options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json",]);
});

builder.Services.Configure<GzipCompressionProviderOptions>(options => {
	options.Level = CompressionLevel.SmallestSize;
});

builder.Services.AddRateLimiter(options => {
	options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
		RateLimitPartition.GetFixedWindowLimiter(
			httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
			partition => new FixedWindowRateLimiterOptions {
				AutoReplenishment = true,
				PermitLimit = 100,
				Window = TimeSpan.FromMinutes(1),
			}
		)
	);
	options.OnRejected = async (context, token) => {
		context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
		await context.HttpContext.Response.WriteAsync("Too many requests", token);
	};
});

var app = builder.Build();
app.Use(async (context, next) => {
	// Strict Transport Security
	context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

	// Content Security Policy
	context.Response.Headers.Append("Content-Security-Policy",
		"default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';");

	// X-Frame-Options
	context.Response.Headers.Append("X-Frame-Options", "DENY");

	// X-Content-Type-Options
	context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

	// Referrer Policy
	context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

	// X-XSS-Protection
	context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

	// Permissions Policy
	context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), location=(), payment=()");

	// Remove Server header
	context.Response.Headers.Remove("Server");
	context.Response.Headers.Remove("X-Powered-By");

	await next();
});

if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI(c => {
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
		c.RoutePrefix = "api/docs";
		c.DocExpansion(DocExpansion.None);
	});
}

app.UseHttpsRedirection();
app.UseHsts();

app.UseResponseCompression();
app.UseRateLimiter();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseCors(MY_CORS);

app.Run();