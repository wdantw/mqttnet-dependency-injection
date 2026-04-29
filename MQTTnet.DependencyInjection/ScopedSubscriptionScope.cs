using Microsoft.Extensions.DependencyInjection;

namespace MQTTnet.DependencyInjection
{
    internal class ScopedSubscriptionScope<TConsumer> : ISubscriptionScope
        where TConsumer : IMqttConsumer
    {
        private readonly AsyncServiceScope _scope;

        public ScopedSubscriptionScope(IServiceProvider serviceProvider)
            => _scope = serviceProvider.CreateAsyncScope();

        public IMqttConsumer CreateConsumer()
            => _scope.ServiceProvider.GetRequiredService<TConsumer>();

        public ValueTask DisposeAsync()
            => _scope.DisposeAsync();
    }
}
