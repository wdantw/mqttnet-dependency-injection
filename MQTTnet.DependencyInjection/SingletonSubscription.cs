using Microsoft.Extensions.Options;
using MQTTnet.Packets;

namespace MQTTnet.DependencyInjection
{
    internal class SingletonSubscription<TConsumer> : ISubscription
        where TConsumer : IMqttConsumer
    {
        private readonly TConsumer _mqttConsumer;

        public SingletonSubscription(TConsumer mqttConsumer, IOptions<ConsumerFilterOptions<TConsumer>> filterBuilderOptions)
        {
            _mqttConsumer = mqttConsumer;
            Filter = filterBuilderOptions.Value.FilterBuilder.Build();
        }

        public MqttTopicFilter Filter { get; }

        public ISubscriptionScope CreateScope()
            => new SingletonSubscriptionScope(_mqttConsumer);
    }
}
