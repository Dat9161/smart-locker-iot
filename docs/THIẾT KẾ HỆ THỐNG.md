# THIẾT KẾ HỆ THỐNG
## Đề tài: Hệ thống tủ thông minh sử dụng ESP32 (IoT)

---

## 1. KIẾN TRÚC TỔNG THỂ

```
┌─────────────────┐        HTTP/REST        ┌──────────────────────┐        HTTP/REST        ┌─────────────────┐
│   NGƯỜI DÙNG    │ ──────────────────────► │       SERVER         │ ◄────────────────────── │     ESP32       │
│  (Trình duyệt)  │ ◄────────────────────── │   (.NET Web API)     │ ──────────────────────► │  (Phần cứng)    │
└─────────────────┘        JSON             └──────────┬───────────┘        JSON             └─────────────────┘
                                                       │
                                                       │ EF Core
                                                       ▼
                                            ┌──────────────────────┐
                                            │     SQL SERVER       │
                                            │     (Database)       │
                                            └──────────────────────┘
```

**Luồng hoạt động chính:**
1. Người dùng đăng nhập → chọn tủ → thuê tủ qua Web
2. Server xử lý yêu cầu → lưu DB → tạo lệnh `pending`
3. ESP32 polling server mỗi 2 giây → nhận lệnh → mở servo
4. Sau 15 giây → servo đóng → ESP32 báo `done` về server
5. Server cập nhật trạng thái → DB ghi lại lịch sử

---

## 2. CÔNG NGHỆ SỬ DỤNG

### 2.1 Backend
| Hạng mục | Công nghệ | Phiên bản | Ghi chú |
|---|---|---|---|
| Framework | **ASP.NET Core Web API** | .NET 8 | RESTful API |
| Ngôn ngữ | **C#** | C# 12 | |
| ORM | **Entity Framework Core** | 8.x | Code-first migration |
| Authentication | **JWT Bearer Token** | | Xác thực người dùng |
| API Docs | **Swagger / OpenAPI** | | Test & document API |
| Serialization | **System.Text.Json** | | JSON response cho ESP32 |

### 2.2 Database
| Hạng mục | Công nghệ | Ghi chú |
|---|---|---|
| DBMS | **SQL Server** | SQL Server 2022 |
| Quản lý schema | **EF Core Migrations** | Code-first |
| Công cụ quản lý | **SSMS / Azure Data Studio** | |

### 2.3 Frontend
| Hạng mục | Công nghệ | Ghi chú |
|---|---|---|
| Ngôn ngữ | **HTML5, CSS3, JavaScript (ES6+)** | Không dùng framework |
| Gọi API | **Fetch API** | Tích hợp sẵn trong browser |
| Lưu session | **localStorage** | Lưu JWT token |
| CSS Framework | **Bootstrap 5** | Responsive nhanh |

### 2.4 Phần cứng (ESP32)
| Hạng mục | Công nghệ | Ghi chú |
|---|---|---|
| Vi điều khiển | **ESP32** | WiFi + Bluetooth tích hợp |
| Ngôn ngữ lập trình | **C++ (Arduino Framework)** | |
| IDE | **Arduino IDE** hoặc **PlatformIO** | |
| HTTP Client | **HTTPClient library** | Gọi REST API |
| JSON Parser | **ArduinoJson library** | Parse response từ server |
| Actuator | **Servo Motor (SG90)** | Điều khiển đóng/mở tủ |
| Kết nối mạng | **WiFi (802.11 b/g/n)** | Kết nối qua HTTP |

### 2.5 Triển khai (DevOps)
| Hạng mục | Công nghệ | Ghi chú |
|---|---|---|
| Containerization | **Docker** | Đóng gói ứng dụng |
| Orchestration | **Docker Compose** | Quản lý app + DB container |
| Cloud | **Azure Virtual Machine** | Chạy trên Ubuntu Server |
| OS Server | **Ubuntu 22.04 LTS** | Chạy trên Azure VM |
| Reverse Proxy | **Nginx** | Forward traffic vào container |

### 2.6 Công cụ phát triển chung
| Hạng mục | Công nghệ | Ghi chú |
|---|---|---|
| IDE Backend | **Visual Studio 2022** hoặc **VS Code** | |
| IDE Frontend | **VS Code** | |
| IDE ESP32 | **Arduino IDE 2.x** | |
| API Testing | **Postman** | Test tất cả endpoint |
| Version Control | **Git + GitHub** | Quản lý source code |
| Git Workflow | **Feature Branch** | Mỗi người 1 nhánh |

---

## 3. THIẾT KẾ DATABASE

### Bảng `Users`
```sql
Users (
    Id          INT           PRIMARY KEY IDENTITY,
    Username    NVARCHAR(50)  NOT NULL UNIQUE,
    Password    NVARCHAR(256) NOT NULL,       -- BCrypt hash
    FullName    NVARCHAR(100) NOT NULL,
    CreatedAt   DATETIME      DEFAULT GETDATE()
)
```

### Bảng `Lockers`
```sql
Lockers (
    Id          INT           PRIMARY KEY IDENTITY,
    Name        NVARCHAR(50)  NOT NULL,        -- Ví dụ: "Tủ A1"
    Status      NVARCHAR(20)  DEFAULT 'available',  -- available | occupied
    UpdatedAt   DATETIME      DEFAULT GETDATE()
)
```

### Bảng `Commands`
```sql
Commands (
    Id          INT           PRIMARY KEY IDENTITY,
    LockerId    INT           FOREIGN KEY → Lockers(Id),
    Action      NVARCHAR(20)  NOT NULL,        -- open | close
    Status      NVARCHAR(20)  DEFAULT 'pending',  -- pending | done
    CreatedAt   DATETIME      DEFAULT GETDATE(),
    ExecutedAt  DATETIME      NULL
)
```

### Bảng `RentalHistory`
```sql
RentalHistory (
    Id          INT           PRIMARY KEY IDENTITY,
    UserId      INT           FOREIGN KEY → Users(Id),
    LockerId    INT           FOREIGN KEY → Lockers(Id),
    RentedAt    DATETIME      DEFAULT GETDATE(),
    ReturnedAt  DATETIME      NULL,
    Status      NVARCHAR(20)  DEFAULT 'active'   -- active | completed
)
```

---

## 4. THIẾT KẾ API

### Auth
| Method | Endpoint | Mô tả | Dùng bởi |
|---|---|---|---|
| POST | `/api/auth/login` | Đăng nhập, trả về JWT token | Frontend |

### Lockers
| Method | Endpoint | Mô tả | Dùng bởi |
|---|---|---|---|
| GET | `/api/lockers` | Lấy danh sách tủ + trạng thái | Frontend |
| POST | `/api/lockers/rent` | Thuê tủ, tạo pending command | Frontend |
| GET | `/api/lockers/history` | Lịch sử thuê tủ của user | Frontend |

### Commands (ESP32)
| Method | Endpoint | Mô tả | Dùng bởi |
|---|---|---|---|
| GET | `/api/command/pending/{lockerId}` | Lấy lệnh chờ | ESP32 |
| POST | `/api/command/done/{id}` | Xác nhận đã thực thi | ESP32 |

### Response mẫu cho ESP32
```json
// GET /api/command/pending/1 → Có lệnh
{
  "id": 5,
  "lockerId": 1,
  "action": "open"
}

// GET /api/command/pending/1 → Không có lệnh
HTTP 204 No Content
```

---

## 5. LUỒNG XỬ LÝ CHI TIẾT

### Luồng thuê tủ (Happy Path)
```
Frontend          Server              Database           ESP32
   │                 │                    │                 │
   ├─POST /rent──────►│                    │                 │
   │                 ├─Check locker status►│                 │
   │                 │◄─available──────────┤                 │
   │                 ├─Insert Command──────►│                 │
   │                 ├─Update Locker=occupied►│              │
   │                 ├─Insert RentalHistory►│                │
   │◄─200 OK─────────┤                    │                 │
   │                 │                    │    (sau 2s)      │
   │                 │◄──GET /pending/1───────────────────── │
   │                 ├──200 {action:"open"}──────────────────►│
   │                 │                    │  servo.write(90) │
   │                 │                    │  delay(15000)    │
   │                 │                    │  servo.write(0)  │
   │                 │◄──POST /done/5──────────────────────── │
   │                 ├─Update Command=done►│                 │
   │                 ├─Update Locker=available►│             │
   │                 ├─Update RentalHistory──►│              │
```

---

## 6. CẤU TRÚC PROJECT

### Backend (.NET)
```
SmartLocker.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── LockersController.cs
│   └── CommandController.cs
├── Services/
│   ├── IAuthService.cs / AuthService.cs
│   ├── ILockerService.cs / LockerService.cs
│   └── ICommandService.cs / CommandService.cs
├── Models/
│   ├── User.cs
│   ├── Locker.cs
│   ├── Command.cs
│   └── RentalHistory.cs
├── DTOs/
│   ├── LoginRequest.cs / LoginResponse.cs
│   ├── RentRequest.cs
│   └── CommandDto.cs
├── Data/
│   └── AppDbContext.cs
├── Dockerfile
└── appsettings.json
```

### Frontend
```
frontend/
├── index.html          (trang đăng nhập)
├── lockers.html        (danh sách tủ)
├── history.html        (lịch sử)
├── css/
│   └── style.css
└── js/
    ├── api.js          (các hàm gọi API)
    ├── auth.js
    ├── lockers.js
    └── history.js
```

### ESP32
```
esp32/
└── smart_locker.ino    (1 file Arduino duy nhất)
    ├── WiFi setup
    ├── polling loop()
    ├── parseCommand()
    └── controlServo()
```

---

## 7. QUY ƯỚC CHUNG CỦA NHÓM

### API Response format chuẩn
```json
// Thành công
{ "success": true, "data": { ... } }

// Thất bại
{ "success": false, "message": "Lý do lỗi" }
```

### Git Branch
```
main          ← nhánh chính, chỉ merge khi ổn định
dev           ← nhánh tích hợp
feature/tv1   ← TV1 làm việc
feature/tv2   ← TV2 làm việc
feature/tv3   ← TV3 làm việc
feature/tv4   ← TV4 làm việc
```

### Port mặc định (local)
| Service | Port |
|---|---|
| Backend API | `5000` |
| SQL Server | `1433` |
| Frontend | Mở trực tiếp file HTML hoặc Live Server |

---

> **Lưu ý:** Tài liệu này là chuẩn chung của cả nhóm. Mọi thay đổi về công nghệ hoặc thiết kế cần được nhóm thống nhất trước khi áp dụng.
