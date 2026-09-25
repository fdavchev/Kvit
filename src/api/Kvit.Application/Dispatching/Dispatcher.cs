using Kvit.Domain.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Kvit.Application.Dispatching
{
    public sealed class Dispatcher(IServiceProvider _serviceProvider) : IDispatcher
    {
        public Task<Result> Send<TCommand>(TCommand command, CancellationToken cancellationToken)
        {
            ICommandHandler<TCommand> handler = ResolveSingleHandler<ICommandHandler<TCommand>>(typeof(TCommand));
            return handler.Handle(command, cancellationToken);
        }

        public Task<Result<TResult>> Send<TCommand, TResult>(TCommand command, CancellationToken cancellationToken)
        {
            ICommandHandler<TCommand, TResult> handler = ResolveSingleHandler<ICommandHandler<TCommand, TResult>>(typeof(TCommand));
            return handler.Handle(command, cancellationToken);
        }

        public Task<Result<TResult>> Query<TQuery, TResult>(TQuery query, CancellationToken cancellationToken)
        {
            IQueryHandler<TQuery, TResult> handler = ResolveSingleHandler<IQueryHandler<TQuery, TResult>>(typeof(TQuery));
            return handler.Handle(query, cancellationToken);
        }

        private THandler ResolveSingleHandler<THandler>(Type requestType)
            where THandler : notnull
        {
            THandler[] handlers = [.. _serviceProvider.GetServices<THandler>()];
            return handlers.Length switch
            {
                1 => handlers[0],
                0 => throw new InvalidOperationException($"No handler is registered for '{requestType.FullName}'. Add one public class that implements the matching handler interface in Kvit.Application."),
                _ => throw new InvalidOperationException($"{handlers.Length} handlers are registered for '{requestType.FullName}', but exactly one is allowed: {string.Join(", ", handlers.Select(handler => handler.GetType().FullName))}."),
            };
        }
    }
}
