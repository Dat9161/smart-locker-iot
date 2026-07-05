#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <ESP32Servo.h>

// -- Cau hinh WiFi ---------------------------------------------------
const char* WIFI_SSID     = "He";
const char* WIFI_PASSWORD = "123456789";

// -- Cau hinh Server -------------------------------------------------
const char* SERVER_IP   = "172.20.10.2";
const int   SERVER_PORT = 5167;

// -- ID tu quan ly ---------------------------------------------------
const int LOCKER_ID_1 = 1;
const int LOCKER_ID_2 = 2;

// -- Chan GPIO -------------------------------------------------------
const int SERVO_PIN_1   = 13;
const int SERVO_PIN_2   = 14;
const int PIN_LED_GREEN = 2;
const int PIN_LED_RED   = 4;

// -- Hang so ---------------------------------------------------------
const int POLL_INTERVAL_MS  = 2000;
const int SERVO_OPEN_DEG    = 90;
const int SERVO_CLOSE_DEG   = 0;
const int DOOR_OPEN_TIME_MS = 15000;

Servo servo1;
Servo servo2;

// -- Ham tien ich ----------------------------------------------------

void setLED(bool isOpen) {
  digitalWrite(PIN_LED_GREEN, isOpen ? HIGH : LOW);
  digitalWrite(PIN_LED_RED,   isOpen ? LOW  : HIGH);
}

void connectWiFi() {
  Serial.print("Connecting to WiFi: ");
  Serial.println(WIFI_SSID);
  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  int retries = 0;
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    retries++;
    if (retries > 40) {
      Serial.println("\nKhong ket noi duoc WiFi. Restart...");
      ESP.restart();
    }
  }

  Serial.println("\nWiFi connected!");
  Serial.print("IP ESP32: ");
  Serial.println(WiFi.localIP());
}

int getPendingCommand(int lockerId, String &action) {
  if (WiFi.status() != WL_CONNECTED) { connectWiFi(); return -1; }

  HTTPClient http;
  String url = "http://" + String(SERVER_IP) + ":" + String(SERVER_PORT)
               + "/api/command/pending/" + String(lockerId);
  http.begin(url);
  int httpCode = http.GET();

  if (httpCode == 200) {
    String payload = http.getString();
    JsonDocument doc;
    deserializeJson(doc, payload);
    action = doc["action"].as<String>();
    int cmdId = doc["id"].as<int>();
    http.end();
    return cmdId;
  }

  http.end();
  return -1; // 204 = khong co lenh
}

void markCommandDone(int commandId) {
  if (WiFi.status() != WL_CONNECTED) { connectWiFi(); return; }

  HTTPClient http;
  String url = "http://" + String(SERVER_IP) + ":" + String(SERVER_PORT)
               + "/api/command/done/" + String(commandId);
  http.begin(url);
  http.addHeader("Content-Type", "application/json");
  int httpCode = http.POST("{}");
  Serial.print("markDone ["); Serial.print(commandId);
  Serial.print("] -> HTTP "); Serial.println(httpCode);
  http.end();
}

void executeOpenCommand(Servo &servo, int commandId, int lockerId) {
  Serial.print("Mo tu #"); Serial.println(lockerId);
  servo.write(SERVO_OPEN_DEG);
  setLED(true);
  delay(DOOR_OPEN_TIME_MS);
  servo.write(SERVO_CLOSE_DEG);
  setLED(false);
  Serial.print("Dong tu #"); Serial.println(lockerId);
  markCommandDone(commandId);
}

// -- Setup -----------------------------------------------------------
void setup() {
  Serial.begin(115200);

  pinMode(PIN_LED_GREEN, OUTPUT);
  pinMode(PIN_LED_RED,   OUTPUT);
  setLED(false);

  servo1.attach(SERVO_PIN_1);
  servo2.attach(SERVO_PIN_2);
  servo1.write(SERVO_CLOSE_DEG);
  servo2.write(SERVO_CLOSE_DEG);

  connectWiFi();
}

// -- Loop ------------------------------------------------------------
void loop() {
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("WiFi mat ket noi. Reconnecting...");
    connectWiFi();
  }

  String action1;
  int cmdId1 = getPendingCommand(LOCKER_ID_1, action1);
  if (cmdId1 != -1 && (action1 == "open" || action1 == "return")) {
    executeOpenCommand(servo1, cmdId1, LOCKER_ID_1);
  }

  String action2;
  int cmdId2 = getPendingCommand(LOCKER_ID_2, action2);
  if (cmdId2 != -1 && (action2 == "open" || action2 == "return")) {
    executeOpenCommand(servo2, cmdId2, LOCKER_ID_2);
  }

  delay(POLL_INTERVAL_MS);
}
