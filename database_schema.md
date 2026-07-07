# Cấu trúc Cơ sở dữ liệu - PrintNow

Tài liệu này tổng hợp cấu trúc cơ sở dữ liệu cho hệ thống PrintNow, được chia thành 4 module chính.

## 1. Module Tài khoản & Tiệm in

### Bảng `Users`
Lưu thông tin đăng nhập và thông tin cá nhân của người dùng (khách hàng hoặc chủ tiệm).
- **Id**: Khóa chính (PK), kiểu Guid/Int.
- **FullName**: Họ và tên đầy đủ.
- **Phone**: Số điện thoại liên hệ.
- **Email**: Địa chỉ email đăng nhập.
- **PasswordHash**: Mật khẩu đã được mã hóa.
- **Role**: Vai trò người dùng (`Customer` / `ShopOwner`).
- **CreatedAt**: Thời gian tạo tài khoản.

### Bảng `Shops`
Lưu thông tin chi tiết của các tiệm in.
- **Id**: Khóa chính (PK), kiểu Int.
- **OwnerId**: Khóa ngoại (FK) liên kết đến bảng `Users` (chủ tiệm).
- **ShopName**: Tên tiệm in.
- **AddressText**: Địa chỉ chi tiết.
- **Latitude**: Vĩ độ.
- **Longitude**: Kinh độ.
- **IsActive**: Trạng thái hoạt động (Bật/Tắt).
- **CreatedAt**: Thời gian tạo.

### Bảng `ShopOperatingHours`
Lưu lịch mở cửa theo ngày trong tuần của từng tiệm in.
- **Id**: Khóa chính (PK), kiểu Int.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **DayOfWeek**: Ngày trong tuần (0-6, ví dụ: 0 là Chủ Nhật).
- **OpenTime**: Giờ mở cửa.
- **CloseTime**: Giờ đóng cửa.
- **IsClosed**: Trạng thái đóng cửa ngày hôm đó (`BIT`).

---

## 2. Module Dịch vụ & Bảng giá

### Bảng `Services`
Lưu cấu hình dịch vụ in ấn và gia công của từng tiệm.
- **Id**: Khóa chính (PK), kiểu Int.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **ServiceCode**: Mã chuẩn của dịch vụ (VD: `A4_BW`, `A4_COLOR`, `A3_BW`, `A5_COLOR`). Kiểu VARCHAR. Để `NULL` nếu là dịch vụ gia công tự do.
- **ServiceName**: Tên hiển thị của dịch vụ (VD: "In màu A4", "Đóng gáy xoắn"). Kiểu NVARCHAR.
- **BasePrice**: Đơn giá dịch vụ. Kiểu DECIMAL.
- **Unit**: Đơn vị tính ("trang" hoặc "cuốn"). Kiểu NVARCHAR.
- **IsActive**: Trạng thái Bật/Tắt dịch vụ. Kiểu BIT.
- **Type**: Loại dịch vụ. Có thể là `Core` (In tài liệu) hoặc `AddOn` (Gia công). Kiểu VARCHAR.

---

## 3. Module Đơn hàng

### Bảng `Orders`
Lưu thông tin tổng quan về đơn hàng.
- **Id**: Khóa chính (PK), kiểu Int.
- **CustomerId**: Khóa ngoại (FK) liên kết đến bảng `Users` (Khách hàng).
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **TotalAmount**: Tổng tiền đơn hàng.
- **Status**: Trạng thái đơn hàng (`Pending`, `Confirmed`, `Printing`, `Ready`, `Completed`, `Cancelled`).
- **CancelReason**: Lý do hủy đơn (nếu có). Kiểu NVARCHAR.
- **PaymentStatus**: Trạng thái thanh toán (`Unpaid`, `Paid`, `Refunded`).
- **PaymentMethod**: Phương thức thanh toán.
- **CreatedAt**: Thời gian tạo đơn.

### Bảng `OrderDetails`
Chi tiết file in và các tùy chọn cho mỗi đơn hàng.
- **Id**: Khóa chính (PK), kiểu Int.
- **OrderId**: Khóa ngoại (FK) liên kết đến bảng `Orders`.
- **ServiceId**: Khóa ngoại (FK) liên kết đến bảng `Services`.
- **FileUrl**: Đường dẫn tải file in.
- **FileName**: Tên file.
- **Quantity**: Số lượng bộ.
- **TotalPages**: Tổng số trang (VD: số trang PDF).
- **PaperSize**: Khổ giấy (`A3`, `A4`, `A5`).
- **IsColor**: In màu hay trắng đen (`BIT`).
- **Sides**: Số mặt in (1 hoặc 2 mặt).
- **AddOnList**: Danh sách các dịch vụ gia công khách đã chọn (VD: "Đóng gáy, Bấm lỗ"). Kiểu NVARCHAR.
- **SubTotal**: Thành tiền cho chi tiết đơn này.

---

## 4. Module Giao tiếp & Đánh giá

### Bảng `Reviews`
Lưu đánh giá và nhận xét của khách hàng sau khi hoàn thành đơn hàng.
- **Id**: Khóa chính (PK), kiểu Int.
- **OrderId**: Khóa ngoại (FK) liên kết đến bảng `Orders`.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **CustomerId**: Khóa ngoại (FK) liên kết đến bảng `Users`.
- **Rating**: Điểm đánh giá (1-5 sao). Kiểu TINYINT.
- **Comment**: Nội dung nhận xét.
- **CreatedAt**: Thời gian đánh giá.

### Bảng `Conversations`
Lưu thông tin về phòng chat giữa khách hàng và tiệm in.
- **Id**: Khóa chính (PK), kiểu Guid.
- **CustomerId**: Khóa ngoại (FK) liên kết đến bảng `Users` (Khách hàng).
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **CreatedAt**: Thời gian tạo phòng chat.

### Bảng `Messages`
Lưu chi tiết nội dung tin nhắn trong các phòng chat.
- **Id**: Khóa chính (PK), kiểu BIGINT.
- **ConversationId**: Khóa ngoại (FK) liên kết đến bảng `Conversations`.
- **SenderId**: Khóa ngoại (FK) liên kết đến bảng `Users` (người gửi).
- **Content**: Nội dung tin nhắn.
- **IsRead**: Trạng thái đã đọc hay chưa.
- **CreatedAt**: Thời gian gửi tin nhắn.
