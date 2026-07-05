# CHƯƠNG 4: LẬP TRÌNH VÀ TRIỂN KHAI

---

## 4.1 Lập trình Backend (.NET 10)

### 4.1.1 Cấu trúc project

Backend được tổ chức theo mô hình phân tầng Controller – Service – Data, giúp tách biệt rõ ràng trách nhiệm của từng lớp. Tầng Controller tiếp nhận HTTP request và trả về response; tầng Service chứa toàn bộ logic nghiệp vụ; tầng Data (EF Core) đảm nhiệm truy cập cơ sở dữ liệu.

```
SmartLocker.API/
├── Controllers/   AuthController, LockersController, CommandController
├── Services/      IAuthService, ILockerService, ICommandService (+ implementations)
├── Models/        User, Locker, Command, RentalHistory
├── DTOs/          LoginRequest/Response, RentRequest, CommandDto, LockerDto, ...
├── Data/          AppDbContext, DbSeeder
├── Migrations/
└── Program.cs
```

Các service được đăng ký vào DI container với vòng đời Scoped trong `Program.cs` và inject vào Controller qua Primary Constructor của C# 12:

```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILockerService, LockerService>();
builder.Services.AddScoped<ICommandService, CommandService>();
```

### 4.1.2 Database và Migration

Hệ thống sử dụng EF Core Code-first, nghĩa là cấu trúc bảng trong SQL Server được sinh tự động từ các entity class C#. `AppDbContext` khai báo bốn `DbSet` tương ứng với bốn bảng và cấu hình ràng buộc trong `OnModelCreating`. Hai bản ghi tủ ban đầu được nhúng vào migration qua `HasData` để đảm bảo nhất quán giữa các môi trường.

Khi server khởi động, `Program.cs` tự động chạy migration và seed dữ liệu mà không cần thao tác thủ công:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db);
}
```

`DbSeeder` kiểm tra `AnyAsync()` trước khi insert để tránh tạo trùng khi server khởi động lại. Mật khẩu tài khoản admin được hash bằng BCrypt trước khi lưu vào database.

### 4.1.3 Xác thực JWT

Sau khi đăng nhập thành công, server cấp JWT token được ký bằng HMAC-SHA256, chứa claim `NameIdentifier` (userId) và có thời hạn 8 giờ. Client gửi token này trong header `Authorization: Bearer <token>` ở mỗi request tiếp theo. Server xác minh token mà không cần truy vấn database, giúp giảm tải so với session truyền thống.

`LockersController` được gắn `[Authorize]` ở cấp class để bảo vệ toàn bộ các endpoint. Ngược lại, `CommandController` không yêu cầu xác thực vì ESP32 là thiết bị nhúng không có cơ chế quản lý token — đây là sự đánh đổi chấp nhận được trong phạm vi triển khai nội bộ.

Khi đăng ký, mật khẩu được hash bằng `BCrypt.HashPassword()` trước khi lưu. Khi đăng nhập, `BCrypt.Verify()` so sánh mật khẩu nhập vào với hash đã lưu mà không cần giải mã.

### 4.1.4 Logic nghiệp vụ

**LockerService** xử lý ba nghiệp vụ chính. `RentAsync` kiểm tra trạng thái tủ trước khi thực hiện — nếu tủ đang `occupied`, yêu cầu bị từ chối ngay. Khi hợp lệ, ba thao tác được gom vào một lần `SaveChangesAsync`: tạo lệnh `open` pending cho ESP32, cập nhật tủ thành `occupied`, và ghi bản ghi lịch sử thuê. Cơ chế kiểm tra trạng thái này cũng xử lý race condition một cách tự nhiên khi nhiều người dùng cùng thuê một tủ.

`ReturnAsync` không cập nhật trực tiếp trạng thái tủ mà chỉ tạo lệnh `return` pending. Tủ chỉ thực sự chuyển về `available` sau khi ESP32 báo hoàn thành — đảm bảo trạng thái trong database luôn khớp với trạng thái vật lý.

**CommandService** phục vụ hai đầu giao tiếp với ESP32. `GetPendingCommandAsync` lấy lệnh chờ cũ nhất theo FIFO. `MarkDoneAsync` phân nhánh theo loại lệnh: nếu là `return`, cập nhật tủ về `available` và đóng lịch sử thuê đang active; nếu là `open`, chỉ đánh dấu lệnh done mà không thay đổi trạng thái tủ — tủ vẫn `occupied` vì người dùng chưa trả.

```csharp
cmd.Status = "done";
cmd.ExecutedAt = DateTime.UtcNow;

if (cmd.Action == "return")
{
    cmd.Locker.Status = "available";
    // tìm và đóng RentalHistory đang active
    rental.Status = "completed";
    rental.ReturnedAt = DateTime.UtcNow;
}
```

---

## 4.2 Lập trình Frontend (HTML/CSS/JavaScript)

### 4.2.1 Cấu trúc và tổ chức

Giao diện được xây dựng bằng HTML5, CSS3 và JavaScript ES6+ thuần, không dùng framework frontend để đơn giản hóa quá trình phát triển. Ba trang HTML tương ứng với ba luồng chức năng: `index.html` (đăng nhập/đăng ký), `dashboard.html` (quản lý tủ), `history.html` (lịch sử thuê). Toàn bộ logic JavaScript được tập trung trong một file `app.js` duy nhất, tổ chức thành các hàm `setupLoginPage()`, `setupDashboardPage()`, `setupHistoryPage()` được gọi theo trang hiện tại.

Điểm khởi chạy là sự kiện `DOMContentLoaded`: ứng dụng tự nhận diện trang qua `location.pathname` và kiểm tra token trong `localStorage` — nếu chưa đăng nhập sẽ điều hướng ngay về `index.html`, ngăn chặn truy cập trái phép.

### 4.2.2 API Client và quản lý session

Tất cả yêu cầu HTTP đi qua object `api` tập trung, giúp tránh lặp lại code xử lý HTTP ở nhiều nơi. Object này tự động gắn JWT token vào header `Authorization`, xử lý lỗi 401 bằng cách xóa session và redirect về trang đăng nhập, đồng thời parse JSON và ném lỗi với thông báo từ server.

Session được lưu trong `localStorage` qua hai key: `smartLockerToken` cho JWT token và `smartLockerUser` cho thông tin hiển thị. Khi đăng xuất, cả hai key bị xóa.

### 4.2.3 Trang Dashboard

Khi load, dashboard gọi `GET /api/lockers` và render lưới thẻ tủ. Mỗi thẻ hiển thị trạng thái và nút hành động phù hợp — tủ trống có nút "Thuê tủ", tủ đang dùng có nút "Trả tủ". Khi người dùng xác nhận thuê, trạng thái local được cập nhật ngay lập tức (optimistic update) và render lại mà không cần gọi thêm API, mang lại phản hồi tức thì cho người dùng.

### 4.2.4 Phục vụ giao diện qua Backend

Frontend không cần web server riêng. ASP.NET Core phục vụ trực tiếp các file tĩnh từ thư mục `frontend/` qua middleware `UseStaticFiles`, ánh xạ đến đường dẫn `/app/`. Toàn bộ hệ thống chỉ cần một tiến trình duy nhất và một cổng duy nhất (`5167`).

---

## 4.3 Lập trình ESP32 (Arduino C++)

### 4.3.1 Cấu trúc chương trình

Chương trình ESP32 theo cấu trúc Arduino tiêu chuẩn gồm `setup()` chạy một lần khi khởi động và `loop()` chạy lặp lại liên tục. Toàn bộ thông số cấu hình — WiFi, IP server, chân GPIO, thời gian — được khai báo tập trung bằng hằng số ở đầu file, giúp dễ điều chỉnh khi thay đổi môi trường triển khai.

Bốn thư viện được sử dụng: `WiFi` và `HTTPClient` tích hợp sẵn trong ESP32 Arduino core; `ESP32Servo` để điều khiển servo bằng PWM phần cứng; `ArduinoJson 7.x` để parse JSON từ response của server.

### 4.3.2 Kết nối WiFi và khả năng phục hồi

`connectWiFi()` đặt ESP32 ở chế độ Station (STA) và chờ kết nối tối đa 20 giây. Nếu vượt quá giới hạn này, `ESP.restart()` được gọi để khởi động lại, tránh trạng thái treo vô thời hạn. Trong `loop()`, trạng thái WiFi được kiểm tra ở đầu mỗi chu kỳ — nếu mất kết nối sẽ tự gọi lại `connectWiFi()` để phục hồi.

### 4.3.3 Polling và điều khiển servo

Mỗi 2 giây, `loop()` lần lượt gọi `getPendingCommand()` cho Tủ 1 rồi Tủ 2. Hàm này gửi `GET /api/command/pending/{lockerId}` và parse JSON response bằng ArduinoJson. Server trả về HTTP 200 kèm lệnh nếu có, hoặc HTTP 204 nếu không có lệnh nào chờ.

Khi nhận được lệnh hợp lệ (`open` hoặc `return`), `executeOpenCommand()` thực hiện theo trình tự: xoay servo lên 90° để mở khóa, bật LED xanh, giữ 15 giây, xoay về 0° để đóng, tắt LED xanh bật LED đỏ, rồi gọi `POST /api/command/done/{id}` để báo server. Server nhận tín hiệu này và thực hiện hậu xử lý tương ứng.

```cpp
void executeOpenCommand(Servo &servo, int commandId, int lockerId) {
  servo.write(SERVO_OPEN_DEG);  // mở khóa
  setLED(true);
  delay(DOOR_OPEN_TIME_MS);     // giữ 15 giây
  servo.write(SERVO_CLOSE_DEG); // đóng khóa
  setLED(false);
  markCommandDone(commandId);   // báo server
}
```

LED xanh (GPIO 2) sáng khi tủ đang mở, LED đỏ (GPIO 4) sáng khi tủ đóng hoặc ở trạng thái chờ, giúp quan sát trực tiếp trạng thái vật lý mà không cần nhìn vào giao diện web.

---

## 4.4 Triển khai hệ thống

Hệ thống được triển khai trên môi trường cục bộ với .NET 10, SQL Server 2025 Express và Arduino IDE 2.x. Các thông tin nhạy cảm (Connection String, JWT key) được tách ra file `appsettings.Development.json` không đưa lên git; file mẫu `appsettings.Development.example.json` được cung cấp để thành viên mới có thể tự cấu hình.

Quy trình khởi động gồm ba bước: **(1)** chạy `dotnet run` — server tự migrate database, seed admin và lắng nghe tại `0.0.0.0:5167`; **(2)** cập nhật `SERVER_IP` trong file `.ino` thành IP máy chủ trong LAN, nạp firmware vào ESP32 qua Arduino IDE; **(3)** thêm Inbound Rule trong Windows Firewall cho phép TCP cổng 5167 để ESP32 có thể kết nối từ mạng LAN.

Sau khi hoàn tất, giao diện web truy cập tại `http://localhost:5167/app/` với tài khoản mặc định `admin / admin123`, và tài liệu API đầy đủ có thể xem tại `http://localhost:5167/scalar/v1`.

---

*Kết thúc Chương 4*
