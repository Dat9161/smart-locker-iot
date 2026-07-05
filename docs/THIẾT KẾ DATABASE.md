# THIẾT KẾ DATABASE
## Hệ thống tủ thông minh IoT — Smart Locker

---

## 1. TỔNG QUAN

- **DBMS:** SQL Server 2025 Express
- **Instance:** `localhost\SQLEXPRESS`
- **Database:** `SmartLockerDb`
- **ORM:** Entity Framework Core 9 (Code-first Migration)
- **Số bảng:** 4 bảng chính + 1 bảng migration lịch sử

---

## 2. SƠ ĐỒ QUAN HỆ (ERD)

```
┌─────────────────────┐          ┌─────────────────────────┐
│        Users        │          │         Lockers          │
├─────────────────────┤          ├─────────────────────────┤
│ PK  Id   INT        │          │ PK  Id        INT        │
│     Username        │          │     Name      NVARCHAR   │
│     Password        │          │     Status    NVARCHAR   │
│     FullName        │          │     UpdatedAt DATETIME2  │
│     CreatedAt       │          └────────┬────────┬────────┘
└────────┬────────────┘                   │        │
         │                               │        │
         │  1                         1  │     1  │
         │  ┌────────────────────────────┘        │
         │  │  N                               N  │
         ▼  ▼                                  ▼  │
┌──────────────────────────┐    ┌──────────────────────────┐
│     RentalHistories      │    │         Commands          │
├──────────────────────────┤    ├──────────────────────────┤
│ PK  Id         INT       │    │ PK  Id         INT        │
│ FK  UserId     INT ──────┘    │ FK  LockerId   INT ───────┘
│ FK  LockerId   INT            │     Action     NVARCHAR   │
│     RentedAt   DATETIME2      │     Status     NVARCHAR   │
│     ReturnedAt DATETIME2?     │     CreatedAt  DATETIME2  │
│     Status     NVARCHAR       │     ExecutedAt DATETIME2? │
└──────────────────────────┘    └──────────────────────────┘
```

---

## 3. CHI TIẾT TỪNG BẢNG

### 3.1 Bảng `Users`
Lưu thông tin tài khoản người dùng.

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| Id | INT | PK, IDENTITY(1,1) | Khóa chính tự tăng |
| Username | NVARCHAR(50) | NOT NULL, UNIQUE | Tên đăng nhập |
| Password | NVARCHAR(256) | NOT NULL | Mật khẩu đã hash bằng BCrypt |
| FullName | NVARCHAR(100) | NOT NULL | Họ tên đầy đủ |
| CreatedAt | DATETIME2 | NOT NULL | Thời điểm tạo tài khoản |

**Index:**
- `IX_Users_Username` — UNIQUE INDEX trên cột `Username`

**Giá trị mặc định khi khởi động (DbSeeder):**
| Username | Password | FullName |
|---|---|---|
| admin | *(BCrypt hash của "admin123")* | Quản trị viên |

---

### 3.2 Bảng `Lockers`
Lưu thông tin và trạng thái từng tủ vật lý.

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| Id | INT | PK, IDENTITY(1,1) | Khóa chính tự tăng |
| Name | NVARCHAR(50) | NOT NULL | Tên tủ (vd: "Tủ 1", "Tủ 2") |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'available' | Trạng thái tủ |
| UpdatedAt | DATETIME2 | NOT NULL | Lần cập nhật cuối |

**Giá trị Status:**
| Giá trị | Ý nghĩa |
|---|---|
| `available` | Tủ đang trống, có thể thuê |
| `occupied` | Tủ đang có người sử dụng |

**Seed data:**
| Id | Name | Status |
|---|---|---|
| 1 | Tủ 1 | available |
| 2 | Tủ 2 | available |

---

### 3.3 Bảng `Commands`
Lưu các lệnh điều khiển servo gửi từ server đến ESP32. ESP32 polling bảng này mỗi 2 giây.

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| Id | INT | PK, IDENTITY(1,1) | Khóa chính tự tăng |
| LockerId | INT | FK → Lockers(Id) CASCADE | Tủ cần thực thi lệnh |
| Action | NVARCHAR(20) | NOT NULL | Loại lệnh |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'pending' | Trạng thái lệnh |
| CreatedAt | DATETIME2 | NOT NULL | Thời điểm tạo lệnh |
| ExecutedAt | DATETIME2 | NULL | Thời điểm ESP32 thực thi xong |

**Giá trị Action:**
| Giá trị | Ý nghĩa | Tạo bởi |
|---|---|---|
| `open` | Mở khóa servo khi người dùng thuê tủ | API `/api/lockers/rent` |
| `return` | Mở khóa servo khi người dùng trả tủ | API `/api/lockers/return` |

**Giá trị Status:**
| Giá trị | Ý nghĩa |
|---|---|
| `pending` | Chờ ESP32 thực thi |
| `done` | ESP32 đã thực thi xong |

**Index:**
- `IX_Commands_LockerId` — INDEX trên cột `LockerId`

---

### 3.4 Bảng `RentalHistories`
Lưu lịch sử mỗi lần thuê tủ của người dùng.

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| Id | INT | PK, IDENTITY(1,1) | Khóa chính tự tăng |
| UserId | INT | FK → Users(Id) CASCADE | Người thuê |
| LockerId | INT | FK → Lockers(Id) CASCADE | Tủ được thuê |
| RentedAt | DATETIME2 | NOT NULL | Thời điểm bắt đầu thuê |
| ReturnedAt | DATETIME2 | NULL | Thời điểm trả tủ |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'active' | Trạng thái thuê |

**Giá trị Status:**
| Giá trị | Ý nghĩa |
|---|---|
| `active` | Đang thuê, chưa trả |
| `completed` | Đã trả tủ xong |

**Index:**
- `IX_RentalHistories_UserId` — INDEX trên cột `UserId`
- `IX_RentalHistories_LockerId` — INDEX trên cột `LockerId`

---

## 4. QUAN HỆ GIỮA CÁC BẢNG

| Quan hệ | Loại | Ràng buộc |
|---|---|---|
| Users → RentalHistories | 1 - N | ON DELETE CASCADE |
| Lockers → RentalHistories | 1 - N | ON DELETE CASCADE |
| Lockers → Commands | 1 - N | ON DELETE CASCADE |

---

## 5. LUỒNG DỮ LIỆU

### Khi người dùng thuê tủ (`POST /api/lockers/rent`)
```
1. Kiểm tra Lockers.Status = 'available'
2. INSERT Commands (Action='open', Status='pending')
3. UPDATE Lockers SET Status='occupied'
4. INSERT RentalHistories (Status='active')
```

### Khi ESP32 polling (`GET /api/command/pending/{lockerId}`)
```
SELECT TOP 1 * FROM Commands
WHERE LockerId = @id AND Status = 'pending'
ORDER BY CreatedAt ASC
→ Trả về JSON nếu có, HTTP 204 nếu không có
```

### Khi ESP32 báo done (`POST /api/command/done/{id}`)
```
UPDATE Commands SET Status='done', ExecutedAt=NOW()
IF Action = 'return':
    UPDATE Lockers SET Status='available'
    UPDATE RentalHistories SET Status='completed', ReturnedAt=NOW()
```

### Khi người dùng trả tủ (`POST /api/lockers/return`)
```
1. Kiểm tra Lockers.Status = 'occupied'
2. INSERT Commands (Action='return', Status='pending')
→ ESP32 nhận lệnh, mở servo, sau 15s đóng lại, báo done
→ Khi done: tủ về available + rental completed
```

---

## 6. SCRIPT SQL THAM KHẢO

```sql
-- Xem tất cả tủ và trạng thái
SELECT Id, Name, Status, UpdatedAt FROM Lockers;

-- Xem lệnh đang chờ ESP32
SELECT c.Id, l.Name, c.Action, c.CreatedAt
FROM Commands c JOIN Lockers l ON c.LockerId = l.Id
WHERE c.Status = 'pending'
ORDER BY c.CreatedAt;

-- Xem lịch sử thuê tủ
SELECT u.FullName, l.Name AS Locker, r.RentedAt, r.ReturnedAt, r.Status
FROM RentalHistories r
JOIN Users u ON r.UserId = u.Id
JOIN Lockers l ON r.LockerId = l.Id
ORDER BY r.RentedAt DESC;

-- Xem toàn bộ lệnh theo tủ
SELECT l.Name, c.Action, c.Status, c.CreatedAt, c.ExecutedAt
FROM Commands c JOIN Lockers l ON c.LockerId = l.Id
ORDER BY c.CreatedAt DESC;
```
