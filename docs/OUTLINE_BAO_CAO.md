# OUTLINE BÁO CÁO TIỂU LUẬN
## Đề tài: Hệ thống tủ thông minh sử dụng ESP32 (IoT)
### Môn: Internet of Things | Trường ĐH Giao thông Vận tải TP.HCM

---

## CHƯƠNG 1: TỔNG QUAN

### 1.1 Đặt vấn đề
- Nhu cầu quản lý tủ đồ tại trường học, bệnh viện, văn phòng
- Hạn chế của hệ thống tủ truyền thống (khóa cơ, không theo dõi từ xa)
- Xu hướng IoT trong quản lý tài sản thông minh

### 1.2 Mục tiêu đề tài
- Xây dựng hệ thống tủ thông minh điều khiển từ xa qua web
- Tích hợp phần cứng ESP32 với phần mềm backend .NET
- Lưu trữ lịch sử thuê tủ, xác thực người dùng

### 1.3 Phạm vi đề tài
- 2 tủ vật lý, 1 ESP32 điều khiển 2 servo
- Giao diện web trên trình duyệt máy tính / điện thoại
- Triển khai trong mạng LAN cục bộ (WiFi chung)

### 1.4 Cấu trúc báo cáo
- Mô tả tổng quan 5 chương

---

## CHƯƠNG 2: LÝ THUYẾT

### 2.1 Tổng quan về IoT
- Khái niệm Internet of Things
- Kiến trúc hệ thống IoT (Perception, Network, Application layer)
- Các giao thức phổ biến: HTTP, MQTT, WebSocket

### 2.2 Vi điều khiển ESP32
- Giới thiệu ESP32: CPU dual-core, WiFi 802.11 b/g/n, Bluetooth
- So sánh ESP32 với Arduino, Raspberry Pi
- Lập trình ESP32 bằng Arduino Framework
- Thư viện: HTTPClient, ArduinoJson, ESP32Servo

### 2.3 Servo Motor SG90
- Nguyên lý hoạt động servo
- Điều khiển góc quay bằng PWM
- Ứng dụng trong cơ cấu khóa tủ

### 2.4 RESTful API
- Khái niệm REST, HTTP Methods (GET, POST, PUT, DELETE)
- Định dạng JSON
- Xác thực JWT (JSON Web Token)

### 2.5 Công nghệ Backend — ASP.NET Core
- Giới thiệu .NET 10, C#
- Entity Framework Core — ORM, Code-first Migration
- Dependency Injection
- SQL Server — RDBMS

### 2.6 Công nghệ Frontend
- HTML5, CSS3, JavaScript ES6+
- Fetch API để gọi REST API
- Bootstrap 5 — responsive design

---

## CHƯƠNG 3: PHÂN TÍCH & THIẾT KẾ

### 3.1 Yêu cầu hệ thống
#### 3.1.1 Yêu cầu chức năng
- Đăng ký, đăng nhập tài khoản
- Xem danh sách tủ và trạng thái (trống / đang dùng)
- Thuê tủ — kích hoạt mở servo qua ESP32
- Trả tủ thủ công qua web
- Xem lịch sử thuê tủ
- ESP32 tự động đóng tủ sau 15 giây

#### 3.1.2 Yêu cầu phi chức năng
- Thời gian phản hồi polling ≤ 2 giây
- Bảo mật: mật khẩu BCrypt, xác thực JWT
- Giao diện responsive, hoạt động trên mobile

### 3.2 Kiến trúc hệ thống
- Sơ đồ kiến trúc 3 tầng: Frontend ↔ Backend ↔ Database
- Vị trí ESP32 trong kiến trúc (polling pattern)
- Sơ đồ triển khai mạng (LAN WiFi)

### 3.3 Thiết kế Database
- Sơ đồ ERD: Users, Lockers, Commands, RentalHistories
- Mô tả chi tiết từng bảng, quan hệ khóa ngoại
- Chiến lược seed data

### 3.4 Thiết kế API
- Danh sách endpoint (Auth, Lockers, Commands)
- Request / Response format chuẩn
- Phân quyền: endpoint công khai vs yêu cầu JWT

### 3.5 Thiết kế giao diện
- Wireframe trang đăng nhập / đăng ký
- Wireframe trang danh sách tủ
- Wireframe trang lịch sử

### 3.6 Thiết kế phần cứng
- Sơ đồ kết nối ESP32 — Servo — LED
- Lưu đồ thuật toán polling ESP32

---

## CHƯƠNG 4: LẬP TRÌNH & TRIỂN KHAI

### 4.1 Backend (.NET 10)
#### 4.1.1 Cấu trúc project
- Mô hình Controller → Service → Repository (EF Core)
- Dependency Injection configuration

#### 4.1.2 Database & Migration
- AppDbContext, các Entity model
- EF Core Code-first Migration
- DbSeeder — tạo tài khoản admin lúc khởi động

#### 4.1.3 Xác thực JWT
- Đăng ký: validate, BCrypt hash password
- Đăng nhập: verify hash, generate JWT token
- Middleware xác thực trên các endpoint bảo vệ

#### 4.1.4 Logic nghiệp vụ
- LockerService: GetAll, Rent, Return, History
- CommandService: GetPending, MarkDone
- Xử lý race condition khi nhiều user cùng thuê 1 tủ

### 4.2 Frontend (HTML/CSS/JS)
#### 4.2.1 Cấu trúc file
- index.html: form đăng nhập / đăng ký 2 tab
- dashboard.html: grid hiển thị tủ + modal thuê/trả
- history.html: bảng lịch sử

#### 4.2.2 Kết nối API
- apiFetch helper: tự động gắn JWT header, xử lý 401
- authApi, lockerApi, historyApi

#### 4.2.3 Serve frontend qua backend
- Static files middleware trong ASP.NET Core
- Truy cập qua http://[IP]:5167/app/

### 4.3 ESP32 (Arduino C++)
#### 4.3.1 Kết nối WiFi STA mode
- WiFi.begin(), xử lý reconnect tự động

#### 4.3.2 Polling vòng lặp
- getPendingCommand(): GET /api/command/pending/{id}
- Parse JSON response với ArduinoJson

#### 4.3.3 Điều khiển Servo
- executeOpenCommand(): servo.write(90) → delay(15s) → servo.write(0)
- markCommandDone(): POST /api/command/done/{id}

#### 4.3.4 LED trạng thái
- LED xanh: tủ đang mở
- LED đỏ: tủ đang đóng / chờ

### 4.4 Triển khai
- Môi trường: Windows, .NET 10, SQL Server 2025 Express
- Cấu hình Connection String, JWT settings
- Firewall: mở port 5167 cho ESP32 truy cập

---

## CHƯƠNG 5: ĐÁNH GIÁ & KẾT QUẢ DEMO

### 5.1 Kết quả đạt được
- Hệ thống hoạt động end-to-end: Web → Server → ESP32 → Servo
- Đầy đủ chức năng: đăng ký, đăng nhập, thuê, trả, lịch sử
- Test API: 14/14 test cases PASSED
- Thời gian phản hồi thực tế: ~2 giây (polling interval)

### 5.2 Demo hệ thống
- Ảnh chụp màn hình giao diện web (đăng nhập, dashboard, lịch sử)
- Video demo: thuê tủ → servo mở → tự đóng sau 15s
- Ảnh phần cứng: ESP32 + Servo + mô hình tủ

### 5.3 Đánh giá
#### Ưu điểm
- Kiến trúc rõ ràng, tách biệt tầng
- API chuẩn REST, dễ mở rộng
- Giao diện responsive, thân thiện
- Code ESP32 có xử lý mất mạng, tự reconnect

#### Hạn chế
- Polling mỗi 2 giây — độ trễ cao hơn MQTT
- Chưa có phân quyền Admin / User
- Chưa triển khai HTTPS
- Chưa deploy lên cloud (Azure VM)

### 5.4 Hướng phát triển
- Chuyển polling sang MQTT để giảm độ trễ và tải server
- Thêm phân quyền: Admin quản lý tất cả tủ
- Deploy lên Azure VM với Docker + Nginx
- Thêm thông báo realtime (SignalR / WebSocket)
- Tích hợp thanh toán khi thuê tủ

### 5.5 Kết luận
- Tóm tắt kết quả đạt được so với mục tiêu đề ra
- Bài học kinh nghiệm trong quá trình thực hiện
- Cảm ơn

---

## TÀI LIỆU THAM KHẢO

1. Espressif Systems — ESP32 Technical Reference Manual
2. Microsoft Docs — ASP.NET Core Web API Documentation
3. Microsoft Docs — Entity Framework Core
4. ArduinoJson — Documentation (arduinojson.org)
5. Bootstrap 5 — Official Documentation
6. RFC 7519 — JSON Web Token (JWT)
