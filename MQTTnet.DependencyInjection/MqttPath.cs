namespace MQTTnet.DependencyInjection
{
    public static class MqttPath
    {
        public static string CombineTopicPath(string? basePath, string tailPath)
        {
            if (tailPath.StartsWith(MqttTopicFilterComparer.LevelSeparator))
                return tailPath[1..];

            if (string.IsNullOrWhiteSpace(basePath))
                return tailPath;

            if (basePath.EndsWith(MqttTopicFilterComparer.LevelSeparator))
                basePath = basePath[..^1];

            if (basePath.StartsWith(MqttTopicFilterComparer.LevelSeparator))
                basePath = basePath[1..];

            return basePath + MqttTopicFilterComparer.LevelSeparator + tailPath;
        }

        public static string? GetRelativeTopicName(string fullTopicName, string baseTopicName)
        {
            if (string.IsNullOrWhiteSpace(baseTopicName))
                return fullTopicName;

            if (!fullTopicName.StartsWith(baseTopicName + MqttTopicFilterComparer.LevelSeparator))
                return null;

            return fullTopicName[(baseTopicName.Length + 1)..];
        }
    }
}
