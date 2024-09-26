using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace BlazorSenac;

public class MqttSenac
{
    public static MqttFactory Factory = new();
    private static IMqttClient? _mqttClient; // Tornar a conexão acessível para enviar mensagens
    private static string _entryTopic = "Senac/BAG/ENTRY"; // Definir o tópico de entrada
    private static double _temperature;
    private static double _humidity;

    public static event Action? OnSensorDataUpdated;

    public static double Temperature => _temperature;
    public static double Humidity => _humidity;

    static List<double> TempArray = new();
    static List<double> HumArray = new();

    public static double TempMedia;
    public static double HumMedia;

    public static async Task Start()
    {
        string broker = "zfeemqtt.eastus.cloudapp.azure.com";
        int port = 41883;
        string clientId = "Senac105";
        string exitTopic = "Senac/BAG/EXIT";
        string username = "Senac";
        string password = "Senac";

        _mqttClient = Factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithCredentials(username, password)
            .WithClientId(clientId)
            .Build();

        var connectResult = await _mqttClient.ConnectAsync(options);
        await _mqttClient.SubscribeAsync(exitTopic);

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                string message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                var sensorData = JsonSerializer.Deserialize<SensorData>(message);

                if (sensorData != null)
                {
                    _temperature = sensorData.Temperatura;
                    _humidity = sensorData.Umidade;
                    OnSensorDataUpdated?.Invoke();
                    TempArray.Add(sensorData.Temperatura);
                    if (TempArray.Count >= 10)
                    {
                        TempArray.Remove(TempArray.First());
                    }
                    HumArray.Add(_humidity);
                    if (HumArray.Count >= 10)
                    {
                        HumArray.Remove(HumArray.First());
                    }

                    TempMedia = TempArray.Sum() / TempArray.Count;
                    HumMedia = HumArray.Sum() / HumArray.Count;
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Failed to deserialize JSON: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        };
    }

    // Função para enviar o comando JSON
    public static async Task SendUpdateCommandAsync()
    {
        if (_mqttClient == null || !_mqttClient.IsConnected)
        {
            Console.WriteLine("MQTT client is not connected.");
            return;
        }

        // Criação do payload JSON
        var message = new
        {
            comando = "Atualizar"
        };

        string jsonMessage = JsonSerializer.Serialize(message);

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(_entryTopic)
            .WithPayload(jsonMessage)
            .Build();

        // Publica a mensagem no tópico de entrada
        await _mqttClient.PublishAsync(mqttMessage);
        Console.WriteLine($"Mensagem enviada para {_entryTopic}: {jsonMessage}");
    }
}


public class SensorData
{
    public double Temperatura { get; set; }
    public double Umidade { get; set; }
}
