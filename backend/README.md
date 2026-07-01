# Smart Locker API — Backend

Hệ thống backend cho **Smart Locker IoT** — ASP.NET Core Web API (.NET 10) với SQL Server, JWT Auth, Docker + Nginx.

> **Thành viên 2** phụ trách: API giao tiếp ESP32 + DevOps (Docker / Azure VM)

---

## Mục lục

1. [Cấu trúc project](#cấu-trúc-project)
2. [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
3. [Chạy local (Development)](#chạy-local-development)
4. [Chạy bằng Docker Compose](#chạy-bằng-docker-compose)
5. [Deploy lên Azure VM](#deploy-lên-azure-vm)
6. [API Endpoints](#api-endpoints)
7. [Seed Data mặc định](#seed-data-mặc-định)

---

## Cấu trúc project

```
backend/
├── SmartLocker.API/
│   ├── Controllers/
│   │   ├── AuthController.cs           # POST /api/auth/login
│   │   ├── LockersController.cs        # GET|POST /api/lockers, GET /api/lockers/history
│   │   └── CommandController.cs        # ESP32: GET /api/command/pending/{id}, POST /api/command/done/{id}
│   ├── Services/
│   │   ├── IAuthService.cs / AuthService.cs
│   │   ├── ILockerService.cs / LockerService.cs
│   │   └── ICommandService.cs / CommandService.cs
│   ├── Models/         (User, Locker, Command, RentalHistory)
│   ├── DTOs/           (ApiResponse, LoginRequest/Response, RentRequest, CommandDto, LockerDto, RentalHistoryDto)
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── DbSeeder.cs
│   ├── Migrations/
│   ├── Program.cs
│   ├── appsettings.json                # Production config (SQL Server local)
│   └── appsettings.Development.json    # Dev config (override connection string)
├── Dockerfile                          # Multi-stage build .NET 10
├── docker-compose.yml                  # Dev: API + SQL Server
├── docker-compose.prod.yml             # Prod: API + SQL Server + Nginx
├── nginx.conf                          # Reverse proxy (port 80 → API:8080)
└── README.md
```

---

## Yêu cầu hệ thống

### Phát triển local
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server 2019+ (hoặc chạy qua Docker)

### Deploy
- Docker Engine + Docker Compose v2
- Azure VM (Ubuntu 22.04 LTS)
- Cổng mở trên NSG: **22** (SSH), **80** (HTTP)

---

## Chạy local (Development)

### Bước 1: Cấu hình connection string

Tạo file `appsettings.Development.json` (dựa theo `appsettings.Development.example.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=SmartLockerDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Hoặc dùng SQL Server với SA account:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=SmartLockerDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

### Bước 2: Chạy migration và start

```bash
cd backend/SmartLocker.API
dotnet run
```

App tự động chạy migration + seed data khi khởi động.

API: `http://localhost:5000`
Scalar UI (API docs): `http://localhost:5000/scalar`

---

## Chạy bằng Docker Compose

### Development (API + SQL Server, port 8080)

```bash
cd backend
docker compose up -d --build

# Xem logs
docker compose logs -f api

# Dừng
docker compose down
```

- API: `http://localhost:8080`
- Scalar UI: `http://localhost:8080/scalar`

### Production (API + SQL Server + Nginx, port 80)

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

- API qua Nginx: `http://localhost/api/...`
- Scalar UI: `http://localhost/scalar`

---

## Deploy lên Azure VM

### Bước 1: Tạo Azure VM

1. Đăng nhập [Azure Portal](https://portal.azure.com)
2. Tạo VM: **Ubuntu 22.04 LTS**, kích thước **B2s** (2 vCPU, 4 GB RAM) trở lên
3. Mở cổng trong **Network Security Group**: TCP **22**, TCP **80**

### Bước 2: Cài Docker trên VM

```bash
# SSH vào VM
ssh azureuser@<VM_PUBLIC_IP>

# Cài Docker
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
newgrp docker

# Kiểm tra
docker --version && docker compose version
```

### Bước 3: Upload source code

```bash
# Từ máy local
scp -r ./backend azureuser@<VM_PUBLIC_IP>:~/smart-locker/
```

Hoặc `git clone` trực tiếp trên VM:

```bash
git clone https://github.com/Dat9161/smart-locker-iot.git
cd smart-locker-iot
```

### Bước 4: Deploy

```bash
cd ~/smart-locker-iot/backend

# Chạy production (API + SQL Server + Nginx)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# Kiểm tra
docker compose ps
docker compose logs -f
```

### Bước 5: Kiểm tra hoạt động

```bash
# Health check
curl http://<VM_PUBLIC_IP>/health

# Test login
curl -X POST http://<VM_PUBLIC_IP>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

### Cấu hình domain (tùy chọn)

1. Trỏ DNS domain về IP public của VM
2. Cập nhật `server_name` trong `nginx.conf`:
   ```nginx
   server_name yourdomain.com;
   ```
3. Cài SSL với Certbot:
   ```bash
   sudo apt install certbot python3-certbot-nginx -y
   sudo certbot --nginx -d yourdomain.com
   ```

---

## API Endpoints

### Authentication

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/auth/login` | ❌ | Đăng nhập, trả về JWT token |

```json
// Request
{ "username": "admin", "password": "admin123" }

// Response 200
{ "token": "eyJ...", "fullName": "Quản trị viên", "userId": 1 }
```

---

### Lockers

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/lockers` | ✅ JWT | Danh sách tủ + trạng thái |
| POST | `/api/lockers/rent` | ✅ JWT | Thuê tủ |
| GET | `/api/lockers/history` | ✅ JWT | Lịch sử thuê của user hiện tại |

---

### Commands — dành cho ESP32

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/command/pending/{lockerId}` | ❌ | Lấy lệnh chờ |
| POST | `/api/command/done/{id}` | ❌ | Xác nhận thực thi xong |

```json
// GET /api/command/pending/1 → Có lệnh: HTTP 200
{ "id": 5, "lockerId": 1, "action": "open" }

// GET /api/command/pending/1 → Không có lệnh: HTTP 204 No Content
```

---

## Seed Data mặc định

| Loại | Giá trị |
|------|---------|
| User | `admin` / `admin123` |
| Lockers | Tủ A1, A2, A3, B1, B2, B3 (đều `available`) |

---

## Lưu ý bảo mật (Production)

- Đổi `Jwt:Key` và `SA_PASSWORD` — không dùng giá trị mặc định
- Không commit `appsettings.Development.json` chứa password thật lên Git
- SQL Server chỉ expose nội bộ trong Docker network, không mở port 1433 ra Internet
- Cân nhắc dùng Azure Key Vault hoặc biến môi trường cho secrets production
