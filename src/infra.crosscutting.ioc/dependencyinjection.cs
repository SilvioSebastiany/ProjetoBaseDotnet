using System.Reflection;
using BaseDotNet.Domain.Commands.Behaviors;
using BaseDotNet.Domain.Interfaces;
using BaseDotNet.Domain.Notifications;
using BaseDotNet.Infra.Data.DbContext;
using BaseDotNet.Infra.Data.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseDotNet.Infra.CrossCutting.Ioc
{
    public static class DependencyInjection
    {
        private static readonly Assembly DomainAssembly = typeof(ValidationBehavior<,>).Assembly;

        public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<BaseDotNetContext>(options => options
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
