using API.Utils.Attributes;
using API.Utils.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace API.Utils.DI {
	public static class ServiceCollectionExtensions {
		private const string SEARCH_PATTERN = "API.*.dll";

		public static IServiceCollection AddInjectables(this IServiceCollection services) {

			var assemblies = new List<Assembly>();
			var dllFiles = AppDomain.CurrentDomain.GetProjectAssemblies();

			if (!dllFiles.Any()) {
				return services;
			}

			foreach (var dllFile in dllFiles) {
				try {
					var assembly = Assembly.LoadFrom(dllFile);
					assemblies.Add(assembly);
				} catch (Exception) {
					continue;
				}
			}
			return ProcessAssemblies(services, assemblies);
		}

		private static IServiceCollection ProcessAssemblies(IServiceCollection services, IEnumerable<Assembly> assemblies) {
			foreach (var assembly in assemblies) {
				try {
					var typesWithAttribute = assembly.GetTypes()
													 .Where(type => type.IsClass &&
																   !type.IsAbstract &&
																	type.GetCustomAttribute<InjectableAttribute>() != null);

					foreach (var type in typesWithAttribute) {
						var attribute = type.GetCustomAttribute<InjectableAttribute>();

						var interfaces = type.GetInterfaces()
											.Where(i => i.Namespace?.StartsWith(SEARCH_PATTERN, StringComparison.OrdinalIgnoreCase) == true);

						if (interfaces.Any()) {
							foreach (var interfaceType in interfaces) {
								RegisterService(services, interfaceType, type, attribute.Lifetime);
							}
						} else {
							RegisterService(services, type, type, attribute.Lifetime);
						}
					}
				} catch (Exception) {
					continue;
				}
			}

			return services;
		}

		private static void RegisterService(IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime) {
			switch (lifetime) {
				case ServiceLifetime.Singleton:
					services.AddSingleton(serviceType, implementationType);
					break;
				case ServiceLifetime.Scoped:
					services.AddScoped(serviceType, implementationType);
					break;
				case ServiceLifetime.Transient:
					services.AddTransient(serviceType, implementationType);
					break;
			}
		}
	}
}