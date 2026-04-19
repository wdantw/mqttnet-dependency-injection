namespace MQTTnet.DependencyInjection.Options
{
    public class MqttLifetimeOptions
    {
        /// <summary>
        /// Задержка между переконнектом
        /// </summary>
        public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromMilliseconds(300);
    }
}
