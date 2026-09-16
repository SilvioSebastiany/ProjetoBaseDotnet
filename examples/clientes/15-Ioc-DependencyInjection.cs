// Código real, vivo em src/infra.crosscutting.ioc/dependencyinjection.cs — este arquivo é cópia de leitura, não edite aqui.
using System.Reflection;
using BaseDotnet.Domain.Commands.Behaviors;
using BaseDotnet.Domain.Interfaces;
using BaseDotnet.Domain.Notifications;
using BaseDotnet.Infra.Data.DbContext;
using BaseDotnet.Infra.Data.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseDotnet.Infra.CrossCutting.Ioc
{
    public static class DependencyInjection
    {
        private static readonly Assembly DomainAssembly = typeof(ValidationBehavior<,>).Assembly;

        public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<BaseDotnetContext>(options => options
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

            services.AddScoped<INotificationContext, NotificationContext>();
            services.AddScoped<IClienteRepository, ClienteRepository>();

            services.AddMediatR(configuracao => configuracao.RegisterServicesFromAssembly(DomainAssembly));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            services.AddValidatorsFromAssembly(DomainAssembly);

            return services;
        }
    }
}
