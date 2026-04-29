using MQTTnet.Packets;

namespace MQTTnet.DependencyInjection
{
    public interface ISubscription
    {
        MqttTopicFilter Filter { get; }

        ISubscriptionScope CreateScope();
    }
}
