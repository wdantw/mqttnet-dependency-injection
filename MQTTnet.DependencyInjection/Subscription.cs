using MQTTnet.Packets;

namespace MQTTnet.DependencyInjection
{
    internal class Subscription
    {
        public Subscription(MqttTopicFilter filter, Func<IServiceProvider, IMqttConsumer> consumerFactory)
        {
            Filter = filter;
            ConsumerFactory = consumerFactory;
        }

        public MqttTopicFilter Filter { get; }

        public Func<IServiceProvider, IMqttConsumer> ConsumerFactory { get; }
    }
}
