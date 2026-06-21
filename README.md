# 🔐 Hệ thống tủ thông minh sử dụng ESP32

Đồ án môn **Internet of Things (IoT)**  
Trường Đại học Giao thông Vận tải TP.HCM

---

## 📌 Mô tả

Hệ thống tủ thông minh cho phép người dùng thuê và điều khiển ngăn tủ từ xa thông qua giao diện web. Thiết bị ESP32 kết nối với server qua WiFi để nhận lệnh và điều khiển servo đóng/mở tủ.

**Kiến trúc:** `Người dùng (Web)` ↔ `Server (.NET)` ↔ `ESP32`

---

## 🛠️ Công nghệ

| Tầng | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8, C#, Entity Framework Core |
| Database | SQL Server |
| Frontend | HTML5, CSS3, JavaScript, Bootstrap 5 |
| Phần cứng | ESP32, Servo SG90, Arduino Framework |
| Deploy | Docker, Docker Compose, Azure VM (Ubuntu) |

---

## 📁 Cấu trúc project

```
smart-locker/
├── backend/          # ASP.NET Core Web API
├── frontend/         # HTML/CSS/JS
├── esp32/            # Arduino code cho ESP32
├── docs/             # Tài liệu, thiết kế hệ thống
└── docker-compose.yml
```

---

## 🚀 Hướng dẫn chạy

### 1. Backend
```bash
cd backend
dotnet restore
dotnet run
# API chạy tại: http://localhost:5000
# Swagger tại:  http://localhost:5000/swagger
```

### 2. Frontend
```
Mở file frontend/index.html bằng trình duyệt
hoặc dùng VS Code Live Server
```

### 3. ESP32
```
Mở esp32/smart_locker.ino bằng Arduino IDE
Cài thư viện: HTTPClient, ArduinoJson, ESP32Servo
Cập nhật ssid, password, serverUrl trong file
Upload lên board
```

### 4. Docker (Deploy)
```bash
docker-compose up -d
```

---

## 🌿 Git Branch

| Branch | Mục đích |
|---|---|
| `main` | Nhánh chính, code ổn định |
| `dev` | Nhánh tích hợp |
| `feature/tv1` | Thành viên 1 (Backend core + DB) |
| `feature/tv2` | Thành viên 2 (IoT API + DevOps) |
| `feature/tv3` | Thành viên 3 (ESP32) |
| `feature/tv4` | Thành viên 4 (Frontend + Báo cáo) |

---

## 👥 Thành viên nhóm

| Thành viên | MSSV | Nhiệm vụ |
|---|---|---|
| [Tên TV1] | | Backend core + Database |
| [Tên TV2] | | IoT API + DevOps |
| [Tên TV3] | | ESP32 + Phần cứng |
| [Tên TV4] | | Frontend + Báo cáo |

---

## 📄 Tài liệu

- [Thiết kế hệ thống](docs/THIẾT%20KẾ%20HỆ%20THỐNG.md)
- [Danh sách công việc](docs/DANH%20SÁCH%20CÔNG%20VIỆC%20CẦN%20LÀM.md)
