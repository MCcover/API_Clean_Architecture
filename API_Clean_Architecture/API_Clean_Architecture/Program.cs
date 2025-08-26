using System.Text.Json.Serialization;
using API.API_Clean_Architecture.Filters;
using API.CompositionRoot;
using Microsoft.OpenApi.Models;

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
					Type = ReferenceType.SecurityScheme, Id = "Bearer",
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

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI(c => {
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
		c.RoutePrefix = "api/docs";
	});
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseCors(MY_CORS);

app.Run();