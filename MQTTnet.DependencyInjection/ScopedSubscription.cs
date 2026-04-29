using Microsoft.Extensions.Options;
using MQTTnet.Packets;

namespace MQTTnet.DependencyInjection
{
    internal class ScopedSubscription<TConsumer> : ISubscription
        where TConsumer : IMqttConsumer
    {
        private readonly IServiceProvider _serviceProvider;

        public ScopedSubscription(IServiceProvider serviceProvider, IOptions<ConsumerFilterOptions<TConsumer>> filterBuilderOptions)
        {
            _serviceProvider = serviceProvider;
            Filter = filterBuilderOptions.Value.FilterBuilder.Build();
        }

        public MqttTopicFilter Filter { get; }

        public ISubscriptionScope CreateScope()
            => new ScopedSubscriptionScope<TConsumer>(_serviceProvider);
    }
}
