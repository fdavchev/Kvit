using Kvit.Api.Registers;
using Kvit.Application.Dispatching;
using Kvit.Domain.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Dispatching
{
    public class DispatcherTests
    {
        [Fact]
        public async Task Send_Command_RunsItsHandler()
        {
            using ServiceProvider provider = BuildProvider(services => services.AddTransient<ICommandHandler<RecordCommand>, RecordCommandHandler>());
            using IServiceScope scope = provider.CreateScope();
            IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
            RecordCommand command = new([]);

            Result result = await dispatcher.Send(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            string handledBy = Assert.Single(command.HandledBy);
            Assert.Equal(nameof(RecordCommandHandler), handledBy);
        }

        [Fact]
        public async Task Send_CommandWithResult_ReturnsItsHandlersValue()
        {
            using ServiceProvider provider = BuildProvider(services => services.AddTransient<ICommandHandler<EchoCommand, string>, EchoCommandHandler>());
            using IServiceScope scope = provider.CreateScope();
            IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            Result<string> result = await dispatcher.Send<EchoCommand, string>(new EchoCommand("hello"), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal($"{nameof(EchoCommandHandler)}: hello", result.Value);
        }

        [Fact]
        public async Task Query_ReturnsItsHandlersValue()
        {
            using ServiceProvider provider = BuildProvider(services => services.AddTransient<IQueryHandler<DoubleQuery, int>, DoubleQueryHandler>());
            using IServiceScope scope = provider.CreateScope();
            IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            Result<int> result = await dispatcher.Query<DoubleQuery, int>(new DoubleQuery(21), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public async Task Send_CommandWithoutHandler_ThrowsNamingTheCommand()
        {
            using ServiceProvider provider = BuildProvider(_ => { });
            using IServiceScope scope = provider.CreateScope();
            IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => dispatcher.Send(new UnhandledCommand(), TestContext.Current.CancellationToken));

            Assert.Contains("No handler is registered", exception.Message);
            Assert.Contains(nameof(UnhandledCommand), exception.Message);
        }

        [Fact]
        public async Task Send_CommandWithTwoHandlers_ThrowsInsteadOfPickingOne()
        {
            using ServiceProvider provider = BuildProvider(services =>
            {
                services.AddTransient<ICommandHandler<RecordCommand>, RecordCommandHandler>();
                services.AddTransient<ICommandHandler<RecordCommand>, RecordCommandHandler>();
            });
            using IServiceScope scope = provider.CreateScope();
            IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
            RecordCommand command = new([]);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => dispatcher.Send(command, TestContext.Current.CancellationToken));

            Assert.Contains("2 handlers are registered", exception.Message);
            Assert.Empty(command.HandledBy);
        }

        private static ServiceProvider BuildProvider(Action<IServiceCollection> registerHandlers)
        {
            ServiceCollection services = new();
            services.AddApplication();
            registerHandlers(services);
            return services.BuildServiceProvider(validateScopes: true);
        }
    }
}
