# CHƯƠNG 5: ĐÁNH GIÁ VÀ KẾT QUẢ

---

## 5.1 Kết quả đạt được

Sau quá trình thiết kế và lập trình, hệ thống tủ thông minh đã hoạt động hoàn chỉnh theo luồng end-to-end: người dùng thao tác trên giao diện web, server xử lý và tạo lệnh, ESP32 polling phát hiện lệnh và điều khiển servo mở tủ vật lý, sau đó báo hoàn thành về server để cập nhật trạng thái. Toàn bộ sáu chức năng đề ra tại Chương 3 đều được hiện thực hóa đầy đủ:

| Chức năng | Trạng thái |
|---|---|
| Đăng ký tài khoản | ✅ Hoàn thành |
| Đăng nhập, xác thực JWT | ✅ Hoàn thành |
| Xem danh sách tủ và trạng thái | ✅ Hoàn thành |
| Thuê tủ — kích hoạt servo qua ESP32 | ✅ Hoàn thành |
| Trả tủ thủ công qua web | ✅ Hoàn thành |
| Xem lịch sử thuê tủ | ✅ Hoàn thành |

Hệ thống được kiểm thử bằng bộ test script PowerShell gồm 14 test case, bao phủ toàn bộ luồng hoạt động chính từ đăng nhập, thuê tủ, polling ESP32, báo hoàn thành, đến trả tủ và kiểm tra lịch sử. Kết quả đạt **14/14 test case PASSED**. Bảng dưới tóm tắt nội dung từng nhóm kiểm thử:

| Nhóm | Nội dung kiểm thử | Kết quả |
|---|---|---|
| Auth | Đăng nhập sai mật khẩu → HTTP 401 | PASS |
| Auth | Đăng nhập đúng → HTTP 200 + JWT token | PASS |
| Auth guard | Gọi `/api/lockers` không có token → HTTP 401 | PASS |
| Lockers | Lấy danh sách tủ với token hợp lệ → HTTP 200 + data | PASS |
| ESP32 polling | Polling khi chưa có lệnh → HTTP 204 | PASS |
| Thuê tủ | Thuê tủ trống → HTTP 200, tạo lệnh pending | PASS |
| Thuê tủ | Thuê tủ đang occupied → HTTP 400 | PASS |
| Trạng thái | Tủ chuyển sang `occupied` sau khi thuê | PASS |
| ESP32 polling | Polling sau khi thuê → HTTP 200 + `action=open` | PASS |
| ESP32 done | Báo hoàn thành lần 1 → HTTP 200 | PASS |
| ESP32 done | Báo hoàn thành lần 2 → HTTP 404 (idempotent) | PASS |
| Trạng thái | Tủ chuyển về `available` sau khi done | PASS |
| Lịch sử | Lấy lịch sử thuê → HTTP 200 + có bản ghi | PASS |
| ESP32 polling | Polling sau khi done → HTTP 204 (không còn lệnh) | PASS |

Về hiệu năng, thời gian phản hồi thực tế từ lúc người dùng xác nhận thuê tủ đến khi servo vật lý bắt đầu mở dao động trong khoảng **1 đến 2 giây**, phụ thuộc vào thời điểm ESP32 thực hiện chu kỳ polling tiếp theo. Kết quả này đáp ứng yêu cầu phi chức năng đã đặt ra ở Chương 3 (≤ 2 giây).

---

## 5.2 Demo hệ thống

Phần này mô tả luồng thao tác thực tế khi sử dụng hệ thống. Giao diện web được truy cập tại `http://localhost:5167/app/` từ trình duyệt trên máy tính hoặc điện thoại cùng mạng LAN.

**Trang đăng nhập / đăng ký** cung cấp hai tab chuyển đổi. Người dùng mới điền họ tên, tên đăng nhập và mật khẩu (tối thiểu 6 ký tự) để tạo tài khoản. Sau khi đăng nhập thành công, JWT token được lưu vào `localStorage` và người dùng được điều hướng tự động sang trang dashboard.

**Trang Dashboard** hiển thị lưới các thẻ tủ, mỗi thẻ cho biết tên tủ, trạng thái (đang trống / đang sử dụng) và nút hành động tương ứng. Tủ trống hiển thị biểu tượng 🔓 và nút "Thuê tủ"; tủ đang được sử dụng hiển thị 🔒 và nút "Trả tủ". Thống kê tổng quan (tổng số tủ, số tủ trống, số tủ đang dùng) được hiển thị phía trên lưới.

Khi nhấn "Thuê tủ", hộp thoại xác nhận xuất hiện. Sau khi xác nhận, giao diện cập nhật ngay lập tức và thông báo "Thuê tủ thành công! Tủ đang mở, vui lòng ra tủ." Phía phần cứng, trong vòng tối đa 2 giây, ESP32 nhận lệnh qua polling và servo tương ứng xoay lên 90° để mở khóa — LED xanh bật sáng. Sau 15 giây, servo tự đóng về 0° và LED đỏ sáng trở lại.

**Trang Lịch sử** liệt kê toàn bộ các lần thuê tủ của tài khoản hiện tại, bao gồm tên tủ, thời điểm thuê, thời điểm trả và trạng thái (Đang thuê / Hoàn thành). Dữ liệu được sắp xếp theo thứ tự mới nhất trước.

> *Ảnh chụp màn hình giao diện và ảnh phần cứng thực tế được đính kèm trong phần Phụ lục của báo cáo.*

---

## 5.3 Đánh giá hệ thống

### 5.3.1 Ưu điểm

Kiến trúc ba tầng (Frontend – Backend – Database) kết hợp mô hình Controller – Service trong backend giúp mã nguồn được tổ chức rõ ràng, mỗi thành phần có trách nhiệm riêng biệt và dễ bảo trì. API tuân theo chuẩn REST với định dạng response thống nhất, tạo nền tảng tốt để mở rộng thêm client (ứng dụng mobile, thiết bị IoT khác) mà không cần thay đổi backend.

Về bảo mật, mật khẩu được hash bằng BCrypt với salt ngẫu nhiên, đảm bảo không thể phục hồi mật khẩu gốc dù database bị lộ. Xác thực JWT không lưu trạng thái phía server, phù hợp với kiến trúc stateless của REST API.

Phần mềm ESP32 được thiết kế với khả năng tự phục hồi: khi mất kết nối WiFi, thiết bị tự động thử kết nối lại; nếu thất bại sau 20 giây, hệ thống khởi động lại để tránh trạng thái treo vô thời hạn. Cơ chế này đảm bảo hệ thống phần cứng ổn định trong môi trường mạng không ổn định.

Giao diện web sử dụng dark theme với bố cục responsive, hoạt động tốt trên cả máy tính và điện thoại di động. Các phản hồi trạng thái (toast notification, loading state trên nút bấm) cung cấp trải nghiệm người dùng rõ ràng, tránh nhấn đúp do phản hồi chậm.

### 5.3.2 Hạn chế

Cơ chế polling mỗi 2 giây tạo ra độ trễ tối đa 2 giây từ lúc người dùng thao tác đến khi tủ mở — chấp nhận được trong phạm vi đề tài nhưng chưa đủ đáp ứng các tình huống yêu cầu phản hồi tức thì. Đồng thời, ESP32 liên tục gửi request đến server dù không có lệnh nào, tạo tải không cần thiết.

Hệ thống hiện chưa phân biệt vai trò Admin và User thông thường. Mọi tài khoản đều có quyền thao tác như nhau, không có giao diện quản trị để theo dõi toàn bộ tủ hoặc can thiệp vào trạng thái. API endpoint dành cho ESP32 không yêu cầu xác thực, phù hợp với môi trường LAN nội bộ nhưng sẽ là rủi ro bảo mật nếu triển khai trên mạng công cộng.

Hệ thống chưa triển khai HTTPS, nghĩa là JWT token và dữ liệu truyền qua mạng ở dạng plain text. Đây là hạn chế cần khắc phục trước khi đưa vào môi trường thực tế.

---

## 5.4 Hướng phát triển

Hướng cải tiến quan trọng nhất là thay thế cơ chế polling bằng **MQTT** (Message Queuing Telemetry Transport) — giao thức pub/sub được thiết kế đặc biệt cho IoT. Với MQTT, ESP32 đăng ký lắng nghe một topic cụ thể; khi có lệnh mới, server publish vào topic đó và ESP32 nhận được ngay lập tức, thay vì phải chờ đến chu kỳ polling tiếp theo. Cách này giảm độ trễ xuống dưới 100ms và loại bỏ các request không cần thiết.

Về bảo mật và phân quyền, hệ thống cần bổ sung hai vai trò: Admin có thể xem và quản lý tất cả tủ, còn User chỉ thao tác được tủ của chính mình. Kết hợp với việc triển khai HTTPS (thông qua Let's Encrypt hoặc reverse proxy Nginx), hệ thống sẽ đủ điều kiện vận hành trên môi trường mạng công cộng.

Về hạ tầng, hệ thống có thể được đóng gói bằng Docker và triển khai lên Azure VM hoặc các nền tảng cloud tương đương. Việc này giúp hệ thống trở nên độc lập khỏi máy tính cá nhân, có thể truy cập từ bất kỳ đâu qua internet. Ngoài ra, tích hợp **SignalR** vào frontend sẽ cho phép giao diện web cập nhật trạng thái tủ theo thời gian thực mà không cần người dùng tải lại trang.

---

## 5.5 Kết luận

Đề tài đã xây dựng thành công hệ thống tủ thông minh điều khiển từ xa, tích hợp ba thành phần: giao diện web, backend .NET và thiết bị IoT ESP32. Tất cả mục tiêu đặt ra ban đầu đều được hoàn thành — hệ thống hoạt động ổn định end-to-end, đáp ứng đầy đủ các yêu cầu chức năng và phi chức năng, với 14/14 test case kiểm thử API đều qua.

Quá trình thực hiện đề tài mang lại nhiều kinh nghiệm thực tiễn: từ việc thiết kế giao tiếp giữa thiết bị nhúng và web API theo mô hình polling, đến xây dựng backend chuẩn REST với xác thực JWT và quản lý schema database bằng EF Core Code-first. Quan trọng hơn, nhóm hiểu được sự đánh đổi trong các quyết định kỹ thuật — đơn giản hóa để đảm bảo hệ thống hoạt động được trước, sau đó mới tối ưu dần theo hướng phát triển đã đề ra.

---

## TÀI LIỆU THAM KHẢO

1. Espressif Systems. *ESP32 Technical Reference Manual*. https://www.espressif.com/sites/default/files/documentation/esp32_technical_reference_manual_en.pdf
2. Microsoft. *ASP.NET Core Web API Documentation*. https://learn.microsoft.com/en-us/aspnet/core/web-api
3. Microsoft. *Entity Framework Core*. https://learn.microsoft.com/en-us/ef/core
4. Benoit Blanchon. *ArduinoJson Documentation*. https://arduinojson.org/v7/doc
5. Bootstrap Team. *Bootstrap 5 Official Documentation*. https://getbootstrap.com/docs/5.3
6. Jones M. et al. *RFC 7519 — JSON Web Token (JWT)*. https://www.rfc-editor.org/rfc/rfc7519
