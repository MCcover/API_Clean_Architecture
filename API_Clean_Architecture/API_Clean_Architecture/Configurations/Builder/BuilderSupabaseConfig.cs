using Supabase;

namespace API.API_Clean_Architecture.Configurations.Builder;

public static class BuilderSupabaseConfig {
	public static void ConfigureSupabase(this WebApplicationBuilder builder) {
		builder.Services.AddScoped<Client>(_ =>
			new Client(
				builder.Configuration["Supabase:Url"]!,
				builder.Configuration["Supabase:Key"]!
			)
		);
	}
}