
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace BlazorSenac
{
    public class MessageService
    {
        private IMqttClient _mqttClient;
        private MqttClientOptions _options;
        public static event Action<string>? OnMessageReceived;

        public MessageService(string broker, int port, string clientId, string username, string password)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithCredentials(username, password)
                .WithClientId(clientId)
                .Build();
        }

        public async Task ConnectAsync()
        {
            try
            {
                var result = await _mqttClient.ConnectAsync(_options);
                Console.WriteLine("Connected to MQTT broker.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
            }
        }

        public async Task SubscribeAsync(string topic)
        {
            await _mqttClient.SubscribeAsync(topic);
            _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;
            Console.WriteLine($"Subscribed to topic: {topic}");
        }

        public async Task PublishAsync(string topic, object message)
        {
            var payload = JsonSerializer.Serialize(message);
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();

            await _mqttClient.PublishAsync(mqttMessage);
            Console.WriteLine($"Published message to topic: {topic}");
        }

        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            OnMessageReceived?.Invoke(message);  // Dispara evento para componentes ouvirem

            Console.WriteLine($"Message received: {message}");
            return Task.CompletedTask;
        }

        public async Task DisconnectAsync()
        {
            await _mqttClient.DisconnectAsync();
            Console.WriteLine("Disconnected from MQTT broker.");
        }
    }
}

