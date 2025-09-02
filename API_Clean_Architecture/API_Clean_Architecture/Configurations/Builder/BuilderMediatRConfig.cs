using System.Reflection;
using API.Utils.Reflection;

namespace API.API_Clean_Architecture.Configurations.Builder;

public static class BuilderMediatRConfig {
    public static void ConfigureMediatR(this IHostApplicationBuilder builder) {
        builder.Services.AddMediatR(cnf => {
            var assem = AppDomain.CurrentDomain.GetProjectAssemblies();
            if (assem == null) {
                return;
            }

            var assemblies = assem.Select(Assembly.LoadFrom).ToArray();
            cnf.RegisterServicesFromAssemblies(assemblies);
        });
    }
}