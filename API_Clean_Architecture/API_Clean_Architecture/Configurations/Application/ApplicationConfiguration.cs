using API.API_Clean_Architecture.Middlewares;
using Microsoft.AspNetCore.Builder;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace API.API_Clean_Architecture.Configurations.Application;

public static class ApplicationConfiguration {
    public static WebApplication Configure(this WebApplication app) {
        app.UseCors(ConfigurationConstants.MY_CORS);

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
        return app;
    }
}