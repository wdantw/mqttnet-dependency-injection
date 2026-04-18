namespace MQTTnet.DependencyInjection.Options
{
    public class MqttOptions
    {
        public const string SectionName = "MqttOptions";

        /// <summary>
        /// Адрес сервера Mqtt
        /// </summary>
        public string? TcpAddress { get; set; }

        /// <summary>
        /// Порт сервера Mqtt
        /// </summary>
        public int? TcpPort { get; set; }
    }
}
