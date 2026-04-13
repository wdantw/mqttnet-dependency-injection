using MQTTnet.Packets;

namespace MQTTnet.DependencyInjection
{
    internal class Subscription
    {
        public Subscription(MqttTopicFilter filter)
        {
            Filter = filter;
        }

        public MqttTopicFilter Filter { get; }
    }
}
