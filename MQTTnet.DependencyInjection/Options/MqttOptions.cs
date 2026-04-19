namespace MQTTnet.DependencyInjection.Options
{
    public class MqttOptions
    {
        public const string SectionName = "MqttOptions";

        /// <summary>
        /// Адрес сервера Mqtt
        /// </summary>
        public string TcpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// Порт сервера Mqtt
        /// </summary>
        public int? TcpPort { get; set; }

        /// <summary>
        /// Период механизма KeepAlive
        /// </summary>
        public TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromMilliseconds(300);
    }
}
