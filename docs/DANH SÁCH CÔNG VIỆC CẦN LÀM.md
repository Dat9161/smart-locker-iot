# DANH SÁCH CÔNG VIỆC CẦN LÀM
## Đề tài: Hệ thống tủ thông minh sử dụng ESP32 (IoT)

---

## 📋 TỔNG QUAN HỆ THỐNG

**Kiến trúc:** Người dùng ↔ Server ↔ ESP32  
**Công nghệ:** .NET (Backend) | HTML/CSS/JS (Frontend) | SQL Server (Database) | ESP32 + Servo (Hardware) | Azure VM + Docker (Deployment)

---

## 👥 PHÂN CHIA NHIỆM VỤ 4 THÀNH VIÊN

---

### 🔴 THÀNH VIÊN 1 — Backend Developer
> Phụ trách: API Server (.NET) + Database

**Database (SQL Server)**
- [ ] Thiết kế schema database (ERD)
- [ ] Tạo bảng `Users` — thông tin người dùng
- [ ] Tạo bảng `Lockers` — danh sách tủ + trạng thái (available/occupied)
- [ ] Tạo bảng `Commands` — lệnh điều khiển (pending/done)
- [ ] Tạo bảng `RentalHistory` — lịch sử thuê tủ
- [ ] Viết migration / script tạo database

**API - Controller & Service**
- [ ] API `POST /api/Auth/login` — đăng nhập, xác thực người dùng
- [ ] API `GET /api/Lockers` — lấy danh sách tủ + trạng thái
- [ ] API `POST /api/Lockers/rent` — thuê tủ (tạo pending command)
- [ ] `CommandService.GetPendingCommand(lockerId)`
- [ ] `CommandService.MarkDone(id)`
- [ ] Logic kiểm tra trạng thái tủ (đã thuê / còn trống)
- [ ] Xử lý lỗi và trả về response chuẩn

---

### 🟠 THÀNH VIÊN 2 — Backend Developer (IoT API) + DevOps
> Phụ trách: API giao tiếp ESP32 + Triển khai hệ thống

**API dành cho ESP32**
- [ ] API `GET /api/Command/pending/{lockerId}` — ESP32 polling lệnh chờ
- [ ] API `POST /api/Control/done/{id}` — ESP32 xác nhận thực thi xong
- [ ] API `GET /api/Lockers/history` — xem lịch sử thuê tủ
- [ ] Test API bằng Postman / Swagger
- [ ] Đảm bảo response JSON đúng định dạng ESP32 đọc được

**Triển khai (DevOps)**
- [ ] Viết `Dockerfile` cho backend .NET
- [ ] Cấu hình `docker-compose.yml` (app + SQL Server)
- [ ] Cấu hình Azure Virtual Machine
- [ ] Deploy container lên Azure VM
- [ ] Kiểm tra hệ thống hoạt động end-to-end trên Internet
- [ ] Cấu hình domain / IP public cho ESP32 kết nối

---

### 🟢 THÀNH VIÊN 3 — Phần cứng (ESP32)
> Phụ trách: Lập trình ESP32 + Lắp ráp mạch

**Lập trình ESP32 (Arduino IDE / PlatformIO)**
- [ ] Kết nối WiFi (`WiFi.begin(ssid, password)`)
- [ ] Xử lý reconnect khi mất mạng
- [ ] Gọi API polling `GET /pending/{lockerId}` mỗi ~2 giây
- [ ] Parse JSON response — kiểm tra `"action":"open"`
- [ ] Điều khiển Servo mở tủ (`servo.write(90)`)
- [ ] Delay 15 giây rồi đóng tủ (`servo.write(0)`)
- [ ] Gọi API `POST /done/{id}` báo server sau khi đóng tủ
- [ ] Thêm LED trạng thái (xanh = mở, đỏ = đóng) *(điểm cộng)*
- [ ] Thêm Reed switch xác nhận tủ đóng thật sự *(điểm cộng)*

**Lắp ráp phần cứng**
- [ ] Lắp ESP32 + Servo lên mô hình tủ
- [ ] Đấu dây, kiểm tra nguồn điện
- [ ] Test phần cứng độc lập trước khi tích hợp

---

### 🔵 THÀNH VIÊN 4 — Frontend + Báo cáo
> Phụ trách: Giao diện web + Hoàn thiện tài liệu

**Frontend (HTML/CSS/JavaScript)**
- [ ] Trang **Đăng nhập** — form, gọi API xác thực, lưu session
- [ ] Trang **Danh sách tủ** — hiển thị ngăn tủ + trạng thái theo thời gian thực
- [ ] Trang **Thuê tủ** — chọn tủ, xác nhận, gửi yêu cầu lên server
- [ ] Trang **Lịch sử** — xem lại lịch sử thuê của người dùng
- [ ] Responsive design (hiển thị tốt trên mobile)
- [ ] Kết nối Frontend ↔ Backend API (fetch/axios)

**Báo cáo & Tài liệu**
- [ ] Hoàn thiện báo cáo Word (bổ sung ảnh demo, kết quả thực tế)
- [ ] Vẽ lại sơ đồ kiến trúc hệ thống rõ ràng

---

## 🤝 CÔNG VIỆC CHUNG (Cả nhóm)

- [ ] Họp kick-off — thống nhất công nghệ, cấu trúc project, Git workflow
- [ ] Tạo Git repository, phân nhánh (branch) cho từng người
- [ ] Kiểm thử tích hợp end-to-end (User → Web → Server → ESP32)
- [ ] Review code lẫn nhau trước khi merge
- [ ] Chuẩn bị demo trực tiếp cho thầy (đảm bảo hệ thống chạy ổn định khi demo)

---

## 📌 THỨ TỰ ƯU TIÊN THỰC HIỆN

| Tuần | Công việc |
|------|-----------|
| Tuần 1 | TV1: Thiết kế DB + API cơ bản — TV3: Lắp mạch + test Servo |
| Tuần 2 | TV1+TV2: Hoàn thiện tất cả API — TV4: Bắt đầu Frontend |
| Tuần 3 | TV3: Tích hợp ESP32 ↔ Server — TV4: Hoàn thiện Frontend |
| Tuần 4 | TV2: Deploy Azure — Cả nhóm: Test tích hợp + sửa bug |
| Tuần 5 | Cả nhóm: Hoàn thiện báo cáo Word + Test lần cuối trước khi nộp |

---

## 🔧 CÁC HẠN CHẾ CẦN KHẮC PHỤC (nếu có thời gian)

- [ ] **Bảo mật:** Thêm JWT Authentication, phân quyền người dùng *(TV1+TV2)*
- [ ] **Hiệu năng IoT:** Chuyển từ polling sang giao thức MQTT *(TV2+TV3)*
- [ ] **Xử lý lỗi:** Xử lý mất kết nối mạng, timeout *(TV2+TV3)*

---

> **Ghi chú:** Đánh dấu `[x]` vào ô khi hoàn thành. TV = Thành viên. Cập nhật tên thật của từng người vào tiêu đề mỗi phần.
