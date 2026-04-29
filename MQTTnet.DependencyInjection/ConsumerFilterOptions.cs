namespace MQTTnet.DependencyInjection
{
    internal class ConsumerFilterOptions<TConsumer>
    {
        public MqttTopicFilterBuilder FilterBuilder { get; } = new MqttTopicFilterBuilder();
    }
}
