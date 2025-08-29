using API.API_Clean_Architecture.Filters;

namespace API.API_Clean_Architecture.Configurations.Builder;

public static class BuilderConfigure {
   public static WebApplicationBuilder Configure(this WebApplicationBuilder builder) {
      builder.ConfigureCors();
      builder.ConfigureJson();
      builder.ConfigureSwagger();
      builder.ConfigureResponseCompression();
      builder.ConfigureRateLimiter();
      builder.ConfigureLogging();

      builder.ConfigureControllers();
      return builder;
   } 
}