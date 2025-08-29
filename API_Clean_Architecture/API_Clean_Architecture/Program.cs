using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using API.API_Clean_Architecture.Filters;
using API.API_Clean_Architecture.Middlewares;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Filters;
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

builder.Services.AddControllers(opt => {
	opt.Filters.Add<ModelStateValidationFilter>();
	opt.Filters.Add<ResponseFilter>();
}).AddJsonOptions(options => {
	options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Logger(lc => lc
	    .WriteTo.File(
		    path: Path.Combine(AppContext.BaseDirectory, "Logs")+"/log-.json", 
		    rollingInterval: RollingInterval.Day,
		    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}",
		    shared: true,
		    retainedFileCountLimit: 30
		) 
    )
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.UseCors(MY_CORS);

app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c => {
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
	c.RoutePrefix = "api/docs";
	c.DocExpansion(DocExpansion.None);
});

app.UseHttpsRedirection();
app.UseHsts();

app.UseResponseCompression();
app.UseRateLimiter();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();