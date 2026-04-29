# ќписание
"ќбв€зка" дл€ использовани€ библиотеке MQTTNet с Microsoft Dependency Injection

# »спользование

–егистраци€:

```
services.AddMqtt();
services.ConfigureMqtt(configuration);
services.RegisterMqttConsumerScoped<MyConsumer>(b => b.WithTopic("mqtt-topic-name"));
```

 онсьюмер
```
public class MyConsumer : IMqttConsumer
{
    public MyConsumer(...)
    {
    }

    public Task Handle(MqttApplicationMessage message, CancellationToken cancellationToken)
    {
        // обработка сообщени€ message
    }
}
```

# TODO
* тесты на отправку сообщени€?
* ѕроверка соединени€ через переодический пинг (может и не надо)
* настройка таймаута ожидани€ ответа
* реализовать передачу сигнала о потери и восстановлении св€зи (может и не надо) + тесты
* использование логгера + тесты
* параллельна€ работа разных потоков на одном клиенте (отправка и прием сообщени€, возможно пинг еще) + тесты
* тест - что все сообщени€ перед остановкой хоста были успешно доставлены (был такой тест в основном приложении) возможно не нужен а возможно настройка поведени€