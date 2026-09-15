using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaseDotnet.Domain.Notifications;
using FluentValidation;
using MediatR;

namespace BaseDotnet.Domain.Commands.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse>(
        IEnumerable<IValidator<TRequest>> validators,
        INotificationContext notificationContext)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!validators.Any())
                return await next();

            var contexto = new ValidationContext<TRequest>(request);

            var falhas = validators
                .Select(validator => validator.Validate(contexto))
                .SelectMany(resultado => resultado.Errors)
                .ToList();

            if (falhas.Count == 0)
                return await next();

            foreach (var falha in falhas)
                notificationContext.AddNotification(falha.PropertyName, falha.ErrorMessage);

            return default;
        }
    }
}
