namespace API.API_Clean_Architecture.Configurations.Builder;

public static class BuilderCorsConfig {

    public static void ConfigureCors(this WebApplicationBuilder builder) {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(ConfigurationConstants.MY_CORS, policy =>
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
    }

    public static string PolicyName => ConfigurationConstants.MY_CORS;
}