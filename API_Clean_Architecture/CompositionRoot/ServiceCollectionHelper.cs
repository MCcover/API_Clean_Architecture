using API.Utils.DI;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace API.CompositionRoot {
	public static class ServiceCollectionHelper {
		public static void AddServices(this IServiceCollection service, string? connectionString) {
			//service.AddDbContext<PostgresContext>(options => options.UseNpgsql(connectionString));

			service.AddInjectables();

			service.AddMediatR(cnf => {
				cnf.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
			});
		}
	}
}