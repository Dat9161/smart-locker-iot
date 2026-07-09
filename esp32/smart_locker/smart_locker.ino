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
const int SERVO_PIN_2   = 27;
const int PIN_LED_GREEN = 2;
const int PIN_LED_RED   = 4;

// -- Hang so ---------------------------------------------------------
const int POLL_INTERVAL_MS  = 2000;
const int SERVO_OPEN_DEG    = 90;
const int SERVO_CLOSE_DEG   = 0;
const int DOOR_OPEN_TIME_MS = 15000;

Servo servo1;
Servo servo2;
volatile int openCount = 0;

// -- Ham tien ich ----------------------------------------------------

void updateLED() {
  bool anyOpen = (openCount > 0);
  digitalWrite(PIN_LED_GREEN, anyOpen ? HIGH : LOW);
  digitalWrite(PIN_LED_RED,   anyOpen ? LOW  : HIGH);
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
    Serial.print("Lenh cho [Locker "); Serial.print(lockerId);
    Serial.print("]: id="); Serial.print(cmdId);
    Serial.print(" action="); Serial.println(action);
    return cmdId;
  }

  http.end();
  return -1;
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

// Attach servo chi khi can, detach sau khi xong de tiet kiem dong
void executeOpenCommand(int servoPin, Servo &servo, int commandId, int lockerId) {
  Serial.print(">>> Mo tu #"); Serial.println(lockerId);

  // Attach khi can
  servo.setPeriodHertz(50);
  servo.attach(servoPin, 500, 2400);
  delay(100);

  openCount++;
  updateLED();

  servo.write(SERVO_OPEN_DEG);
  delay(DOOR_OPEN_TIME_MS);
  servo.write(SERVO_CLOSE_DEG);
  delay(500); // cho servo ve vi tri truoc khi detach

  // Detach sau khi xong: khong con giu PWM, tiet kiem dong ~20mA/servo
  servo.detach();

  openCount--;
  updateLED();

  Serial.print("<<< Dong tu #"); Serial.println(lockerId);
  markCommandDone(commandId);
}

// -- Setup -----------------------------------------------------------
void setup() {
  Serial.begin(115200);
  delay(500);

  pinMode(PIN_LED_GREEN, OUTPUT);
  pinMode(PIN_LED_RED,   OUTPUT);
  openCount = 0;
  updateLED();

  ESP32PWM::allocateTimer(0);
  ESP32PWM::allocateTimer(1);

  // Di chuyen servo ve vi tri dong khi khoi dong
  servo1.setPeriodHertz(50);
  servo1.attach(SERVO_PIN_1, 500, 2400);
  servo1.write(SERVO_CLOSE_DEG);
  delay(500);
  servo1.detach();

  servo2.setPeriodHertz(50);
  servo2.attach(SERVO_PIN_2, 500, 2400);
  servo2.write(SERVO_CLOSE_DEG);
  delay(500);
  servo2.detach();

  Serial.println("Servo initialized.");
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
    executeOpenCommand(SERVO_PIN_1, servo1, cmdId1, LOCKER_ID_1);
  }

  String action2;
  int cmdId2 = getPendingCommand(LOCKER_ID_2, action2);
  if (cmdId2 != -1 && (action2 == "open" || action2 == "return")) {
    executeOpenCommand(SERVO_PIN_2, servo2, cmdId2, LOCKER_ID_2);
  }

  delay(POLL_INTERVAL_MS);
}
