#include <ArduinoJson.h>
#include <Arduino_JSON.h>
#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include <Adafruit_AHT10.h>

const char* ssid = "SENAC";           
const char* password = "x1y2z3@snc";    

Adafruit_AHT10 aht;

// Configurações do Broker MQTT
const char* mqtt_server = "zfeemqtt.eastus.cloudapp.azure.com"; 
const int mqtt_port = 41883;               
const char* mqtt_user = "Senac";               
const char* mqtt_password = "Senac";           
const char* mqtt_client = "Senac202";
// Tópico MQTT
const char* mqtt_topic_entry = "Senac/BAG/ENTRY";      

const char* mqtt_topic_exit = "Senac/BAG/EXIT";


WiFiClient espClient;
PubSubClient client(espClient);
 
const int ledPin = D4; // Pino do LED (D4 no ESP8266)
 
void setup_wifi() {
  delay(10);
  Serial.println();
  Serial.print("Conectando-se a ");
  Serial.println(ssid);
 
  WiFi.begin(ssid, password);
 
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
 
  Serial.println("");
  Serial.println("WiFi conectado");
  Serial.println("Endereço IP: ");
  Serial.println(WiFi.localIP());
}
 
void callback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Mensagem recebida [");
  Serial.print(topic);
  Serial.print("] ");
  for (int i = 0; i < length; i++) {
    Serial.print((char)payload[i]);
  }
  Serial.println();
 
  // Converte o payload para string
  String message = "";
  for (int i = 0; i < length; i++) {
    message += (char)payload[i];
  }
 
  for (unsigned int i = 0; i < length; i++) {
    message[i] = (char)payload[i];
  }
  message[length] = '\0';
  Serial.println(message);
  
  
  StaticJsonDocument<200> doc;
  DeserializationError error = deserializeJson(doc, message);

  if (error) {
    Serial.println(error.c_str());
    return;
  }

  float temperatura = doc["Temperatura"];
  float umidade = doc["Umidade"];

  float Temp = doc["MediaTemp"];
  float Umd = doc["MediaUmd"];

 const char* comando = doc["comando"];
  if (comando && strcmp(comando, "Atualizar") == 0) {
   atualizar();
  }

}

 
void reconnect() {
  while (!client.connected()) {
    Serial.print("Tentando conectar ao broker MQTT...");
    if (client.connect(mqtt_client, mqtt_user, mqtt_password)) {
      Serial.println("conectado");
      client.subscribe(mqtt_topic_entry);
    } else {
      Serial.print("falhou, rc=");
      Serial.print(client.state());
      Serial.println(" tentando novamente em 5 segundos");
      delay(5000);
    }
  }
}
 
void setup() {
 
  Serial.begin(115200);
 
  setup_wifi();
 Serial.begin(115200);
  Serial.println("Adafruit AHT10 demo!");

  if (! aht.begin()) {
    Serial.println("Could not find AHT10? Check wiring");
    while (1) delay(10);
  }
  Serial.println("AHT10 found");

  client.setServer(mqtt_server, mqtt_port);
  client.setCallback(callback);
}
unsigned long lastMsg = 0;
float mediaTemp;
float mediaUmd;
void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();

/* Para fazê-lo atualizar sozinho
unsigned long now = millis();
  if (now - lastMsg > 5000) {
    lastMsg = now;

    sensors_event_t humidity, temp;

  aht.getEvent(&humidity, &temp);// populate temp and humidity objects with fresh data

  StaticJsonDocument<200> doc;
  doc["Temperatura"] = temp.temperature ;
  doc["Umidade"] = humidity.relative_humidity;



  char buffer[256];
  serializeJson(doc, buffer);

  client.publish(mqtt_topic, buffer);
  }*/

}

void atualizar(){
  sensors_event_t humidity, temp;

  aht.getEvent(&humidity, &temp);// populate temp and humidity objects with fresh data

  StaticJsonDocument<200> doc;
  doc["Temperatura"] = temp.temperature ;
  doc["Umidade"] = humidity.relative_humidity;

  char buffer[256];
  serializeJson(doc, buffer);

  client.publish(mqtt_topic_exit, buffer);
}