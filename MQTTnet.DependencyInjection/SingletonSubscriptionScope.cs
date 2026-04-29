using Microsoft.Extensions.DependencyInjection;

namespace MQTTnet.DependencyInjection
{
    internal class SingletonSubscriptionScope : ISubscriptionScope
    {
        private readonly IMqttConsumer _mqttConsumer;

        public SingletonSubscriptionScope(IMqttConsumer mqttConsumer)
            => _mqttConsumer = mqttConsumer;

        public IMqttConsumer CreateConsumer()
            => _mqttConsumer;

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
