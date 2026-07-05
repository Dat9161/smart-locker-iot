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
                                            │  (SQL Server 2025    │
                                            │     Express)         │
                                            └──────────────────────┘
```

**Luồng hoạt động chính:**
1. Người dùng đăng ký / đăng nhập → nhận JWT token
2. Chọn tủ → thuê tủ qua Web → server tạo lệnh `pending`
3. ESP32 polling server mỗi 2 giây → nhận lệnh → mở servo
4. Sau 15 giây → servo đóng → ESP32 báo `done` về server
5. Server cập nhật trạng thái → DB ghi lại lịch sử
6. Người dùng có thể trả tủ thủ công qua web nếu cần

---

## 2. CÔNG NGHỆ SỬ DỤNG

### 2.1 Backend
| Hạng mục | Công nghệ | Phiên bản |
|---|---|---|
| Framework | ASP.NET Core Web API | .NET 10 |
| Ngôn ngữ | C# | C# 13 |
| ORM | Entity Framework Core | 9.x |
| Authentication | JWT Bearer Token | |
| API Docs | Scalar UI (OpenAPI) | |
| Password Hash | BCrypt.Net-Next | 4.0.3 |

### 2.2 Database
| Hạng mục | Công nghệ |
|---|---|
| DBMS | SQL Server 2025 Express |
| Quản lý schema | EF Core Migrations (Code-first) |
| Công cụ quản lý | SSMS 22 |

### 2.3 Frontend
| Hạng mục | Công nghệ |
|---|---|
| Ngôn ngữ | HTML5, CSS3, JavaScript (ES6+) |
| Gọi API | Fetch API |
| Lưu session | localStorage (JWT token) |
| CSS Framework | Bootstrap 5.3 |
| Serve | Static files qua ASP.NET Core |

### 2.4 Phần cứng (ESP32)
| Hạng mục | Công nghệ |
|---|---|
| Vi điều khiển | ESP32 Dev Module |
| Ngôn ngữ | C++ (Arduino Framework) |
| IDE | Arduino IDE 2.x |
| HTTP Client | HTTPClient library |
| JSON Parser | ArduinoJson 7.x |
| Actuator | Servo Motor SG90 × 2 (GPIO 13, 12) |
| LED trạng thái | LED xanh (GPIO 2), LED đỏ (GPIO 4) |
| Kết nối mạng | WiFi STA mode — kết nối vào router |

---

## 3. THIẾT KẾ DATABASE

### Bảng `Users`
```sql
Users (
    Id          INT           PRIMARY KEY IDENTITY,
    Username    NVARCHAR(50)  NOT NULL UNIQUE,
    Password    NVARCHAR(256) NOT NULL,       -- BCrypt hash
    FullName    NVARCHAR(100) NOT NULL,
    CreatedAt   DATETIME2     DEFAULT GETDATE()
)
```

### Bảng `Lockers`
```sql
Lockers (
    Id          INT           PRIMARY KEY IDENTITY,
    Name        NVARCHAR(50)  NOT NULL,        -- "Tủ 1", "Tủ 2"
    Status      NVARCHAR(20)  DEFAULT 'available',  -- available | occupied
    UpdatedAt   DATETIME2     DEFAULT GETDATE()
)
```

### Bảng `Commands`
```sql
Commands (
    Id          INT           PRIMARY KEY IDENTITY,
    LockerId    INT           FOREIGN KEY → Lockers(Id),
    Action      NVARCHAR(20)  NOT NULL,        -- open | close
    Status      NVARCHAR(20)  DEFAULT 'pending',  -- pending | done
    CreatedAt   DATETIME2     DEFAULT GETDATE(),
    ExecutedAt  DATETIME2     NULL
)
```

### Bảng `RentalHistories`
```sql
RentalHistories (
    Id          INT           PRIMARY KEY IDENTITY,
    UserId      INT           FOREIGN KEY → Users(Id),
    LockerId    INT           FOREIGN KEY → Lockers(Id),
    RentedAt    DATETIME2     DEFAULT GETDATE(),
    ReturnedAt  DATETIME2     NULL,
    Status      NVARCHAR(20)  DEFAULT 'active'   -- active | completed
)
```

---

## 4. THIẾT KẾ API

### Auth
| Method | Endpoint | Mô tả | Auth |
|---|---|---|---|
| POST | `/api/auth/login` | Đăng nhập, trả JWT token | ❌ |
| POST | `/api/auth/register` | Đăng ký tài khoản mới | ❌ |

### Lockers
| Method | Endpoint | Mô tả | Auth |
|---|---|---|---|
| GET | `/api/lockers` | Lấy danh sách tủ + trạng thái | ✅ JWT |
| POST | `/api/lockers/rent` | Thuê tủ, tạo pending command | ✅ JWT |
| POST | `/api/lockers/return` | Trả tủ thủ công | ✅ JWT |
| GET | `/api/lockers/history` | Lịch sử thuê tủ của user | ✅ JWT |

### Commands (ESP32)
| Method | Endpoint | Mô tả | Auth |
|---|---|---|---|
| GET | `/api/command/pending/{lockerId}` | ESP32 polling lệnh chờ | ❌ |
| POST | `/api/command/done/{id}` | ESP32 xác nhận đã thực thi | ❌ |

### Response mẫu
```json
// GET /api/command/pending/1 → Có lệnh
{ "id": 5, "lockerId": 1, "action": "open" }

// GET /api/command/pending/1 → Không có lệnh
HTTP 204 No Content

// Thành công
{ "success": true, "data": { ... } }

// Thất bại
{ "success": false, "message": "Lý do lỗi" }
```

---

## 5. LUỒNG XỬ LÝ CHI TIẾT

### Luồng thuê tủ
```
Frontend          Server              Database           ESP32
   │                 │                    │                 │
   ├─POST /rent──────►│                    │                 │
   │                 ├─Check locker────── ►│                 │
   │                 │◄─available──────────┤                 │
   │                 ├─Insert Command──────►│                │
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
backend/SmartLocker.API/
├── Controllers/
│   ├── AuthController.cs         -- POST /login, /register
│   ├── LockersController.cs      -- GET, POST /rent, /return, /history
│   └── CommandController.cs      -- GET /pending, POST /done
├── Services/
│   ├── AuthService.cs / IAuthService.cs
│   ├── LockerService.cs / ILockerService.cs
│   └── CommandService.cs / ICommandService.cs
├── Models/
│   ├── User.cs / Locker.cs / Command.cs / RentalHistory.cs
├── DTOs/
│   ├── LoginRequest/Response, RegisterRequest
│   ├── RentRequest, CommandDto, LockerDto, RentalHistoryDto
│   └── ApiResponse.cs
├── Data/
│   ├── AppDbContext.cs
│   └── DbSeeder.cs               -- Seed admin user lúc startup
├── Migrations/
└── Program.cs
```

### Frontend
```
frontend/
├── index.html      -- Đăng nhập / Đăng ký (2 tab)
├── dashboard.html  -- Danh sách tủ + thuê / trả tủ
├── history.html    -- Lịch sử thuê
├── app.js          -- Logic + API calls (Fetch API thật)
└── style.css       -- Dark theme, responsive
```

### ESP32
```
esp32/smart_locker/
└── smart_locker.ino
    ├── connectWiFi()         -- STA mode, auto reconnect
    ├── getPendingCommand()   -- GET /api/command/pending/{id}
    ├── markCommandDone()     -- POST /api/command/done/{id}
    └── executeOpenCommand()  -- servo mở → delay 15s → đóng → báo done
```

---

## 7. CẤU HÌNH MẠNG (LOCAL)

| Service | Địa chỉ |
|---|---|
| Backend API | `http://0.0.0.0:5167` |
| Frontend (qua backend) | `http://localhost:5167/app/index.html` |
| Scalar API Docs | `http://localhost:5167/scalar/v1` |
| SQL Server | `localhost\SQLEXPRESS` |
| WiFi chung | `He` (ESP32 + máy tính cùng mạng) |
| IP máy tính | `172.20.10.2` (ESP32 gọi vào đây) |

---

## 8. QUY ƯỚC NHÓM

### Git Branch
```
main              ← nhánh chính
feature/tv1       ← Backend core + Database
feature/tv2       ← IoT API + DevOps
lehoaibao-fondend ← Frontend
feature/tv3       ← ESP32
```

### Tài khoản mặc định (seed)
| Username | Password | Vai trò |
|---|---|---|
| admin | admin123 | Quản trị viên |
