# Cấu trúc Cơ sở dữ liệu - PrintNow

Tài liệu này tổng hợp cấu trúc cơ sở dữ liệu cho hệ thống PrintNow, được chia thành 6 module chính.

## 1. Module Tài khoản & Tiệm in

### Bảng `Users`
Lưu thông tin đăng nhập và thông tin cá nhân của người dùng.
- **Id**: Khóa chính (PK), kiểu Int.
- **FullName**: Họ và tên đầy đủ.
- **Phone**: Số điện thoại liên hệ.
- **Email**: Địa chỉ email đăng nhập.
- **PasswordHash**: Mật khẩu đã được mã hóa.
- **Role**: Vai trò người dùng (`Customer` / `ShopOwner` / `Admin`).
- **CreatedAt**: Thời gian tạo tài khoản.

### Bảng `Shops`
Lưu thông tin chi tiết của các tiệm in.
- **Id**: Khóa chính (PK), kiểu Int.
- **OwnerId**: Khóa ngoại (FK) liên kết đến bảng `Users` (chủ tiệm).
- **ShopName**: Tên tiệm in.
- **AddressText**: Địa chỉ chi tiết.
- **Latitude**: Vĩ độ.
- **Longitude**: Kinh độ.
- **ImageUrl**: URL hình ảnh tiệm.
- **IsActive**: Trạng thái hoạt động (Bật/Tắt).
- **Description**: Mô tả ngắn về tiệm.
- **OperatingHoursText**: Giờ mở cửa dạng text.
- **BankAccountName**: Tên chủ tài khoản ngân hàng.
- **BankAccountNumber**: Số tài khoản ngân hàng.
- **BankName**: Tên ngân hàng.
- **IsLocked**: Trạng thái khóa (do chưa đóng phí sàn).
- **Balance**: Số dư khả dụng (doanh thu từ đơn hàng sau khi trừ phí nền tảng).
- **CreatedAt**: Thời gian tạo.

### Bảng `ShopOperatingHours`
Lưu lịch mở cửa theo ngày trong tuần của từng tiệm in.
- **Id**: Khóa chính (PK), kiểu Int.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **DayOfWeek**: Ngày trong tuần (0-6).
- **OpenTime**: Giờ mở cửa.
- **CloseTime**: Giờ đóng cửa.
- **IsClosed**: Trạng thái đóng cửa.

---

## 2. Module Dịch vụ & Bảng giá

### Bảng `Services`
Lưu cấu hình dịch vụ in ấn và gia công của từng tiệm.
- **Id**: Khóa chính (PK), kiểu Int.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **ServiceCode**: Mã chuẩn của dịch vụ (VD: `A4_BW`, `A4_COLOR`).
- **ServiceName**: Tên hiển thị của dịch vụ.
- **BasePrice**: Đơn giá dịch vụ.
- **Unit**: Đơn vị tính ("trang" hoặc "cuốn").
- **IsActive**: Trạng thái Bật/Tắt.
- **Type**: Loại dịch vụ (`Core` hoặc `AddOn`).

---

## 3. Module Đơn hàng

### Bảng `Orders`
Lưu thông tin tổng quan về đơn hàng.
- **Id**: Khóa chính (PK), kiểu Int.
- **CustomerId**: Khóa ngoại (FK) liên kết đến bảng `Users`.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **TotalAmount**: Tổng tiền đơn hàng.
- **Status**: Trạng thái (`Pending`, `Confirmed`, `Printing`, `Ready`, `Completed`, `Cancelled`).
- **CancelReason**: Lý do hủy đơn.
- **PaymentStatus**: Trạng thái thanh toán (`Unpaid`, `Paid`, `Refunded`).
- **PaymentMethod**: Phương thức thanh toán (`PayOS`).
- **CreatedAt**: Thời gian tạo đơn.
- **DeliveryMethod**: Phương thức nhận hàng (`Pickup`, `Delivery`).
- **ShippingAddress**: Địa chỉ giao hàng.
- **ShippingFee**: Phí vận chuyển.

### Bảng `OrderDetails`
Chi tiết file in và các tùy chọn cho mỗi đơn hàng.
- **Id**: Khóa chính (PK), kiểu Int.
- **OrderId**: Khóa ngoại (FK) liên kết đến bảng `Orders`.
- **ServiceId**: Khóa ngoại (FK) liên kết đến bảng `Services`.
- **FileUrl**: Đường dẫn tải file in.
- **FileName**: Tên file.
- **Quantity**: Số lượng bộ.
- **TotalPages**: Tổng số trang.
- **PaperSize**: Khổ giấy (`A3`, `A4`, `A5`).
- **IsColor**: In màu hay trắng đen.
- **Sides**: Số mặt in (1 hoặc 2 mặt).
- **AddOnList**: Danh sách các dịch vụ gia công.
- **SubTotal**: Thành tiền.

---

## 4. Module Giao tiếp & Đánh giá

### Bảng `Reviews`
Lưu đánh giá và nhận xét của khách hàng.
- **Id**: Khóa chính (PK), kiểu Int.
- **OrderId**: Khóa ngoại (FK) liên kết đến bảng `Orders`.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **CustomerId**: Khóa ngoại (FK) liên kết đến bảng `Users`.
- **Rating**: Điểm đánh giá (1-5 sao).
- **Comment**: Nội dung nhận xét.
- **CreatedAt**: Thời gian đánh giá.

### Bảng `Conversations`
Lưu thông tin về phòng chat.
- **Id**: Khóa chính (PK), kiểu Guid.
- **CustomerId**: Khóa ngoại (FK) liên kết đến bảng `Users`.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **CreatedAt**: Thời gian tạo phòng chat.

### Bảng `Messages`
Lưu chi tiết nội dung tin nhắn.
- **Id**: Khóa chính (PK), kiểu BIGINT.
- **ConversationId**: Khóa ngoại (FK) liên kết đến bảng `Conversations`.
- **SenderId**: Khóa ngoại (FK) liên kết đến bảng `Users`.
- **Content**: Nội dung tin nhắn.
- **IsRead**: Trạng thái đã đọc.
- **CreatedAt**: Thời gian gửi tin nhắn.

---

## 5. Module Thanh toán & Phí sàn

### Bảng `PaymentTransactions`
Lưu lịch sử giao dịch thanh toán qua PayOS.
- **Id**: Khóa chính (PK), kiểu Int.
- **TransactionType**: Loại giao dịch (`OrderPayment`, `PlatformFee`).
- **OrderId**: Khóa ngoại (FK) tùy chọn đến bảng `Orders`.
- **PlatformFeeId**: Khóa ngoại (FK) tùy chọn đến bảng `PlatformFees`.
- **ShopId**: ID tiệm in liên quan.
- **Amount**: Số tiền giao dịch.
- **PayOSOrderCode**: Mã đơn hàng từ PayOS.
- **PayOSPaymentLinkId**: ID link thanh toán từ PayOS.
- **Status**: Trạng thái (`Pending`, `Completed`, `Failed`, `Cancelled`).
- **PayOSResponse**: Phản hồi từ PayOS (dạng JSON).
- **CreatedAt**: Thời gian tạo.
- **CompletedAt**: Thời gian hoàn thành.

### Bảng `PlatformFees`
Lưu hóa đơn phí sàn hàng tháng cho mỗi tiệm in.
- **Id**: Khóa chính (PK), kiểu Int.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **Month**: Tháng (1-12).
- **Year**: Năm.
- **Amount**: Số tiền phí sàn (199,000đ).
- **Status**: Trạng thái (`Unpaid`, `Paid`, `Cancelled`).
- **PayOSOrderCode**: Mã đơn hàng từ PayOS.
- **CreatedAt**: Thời gian tạo hóa đơn.
- **PaidAt**: Thời gian thanh toán.

### Bảng `WithdrawalRequests`
Lưu yêu cầu rút tiền từ chủ tiệm in.
- **Id**: Khóa chính (PK), kiểu Int.
- **ShopId**: Khóa ngoại (FK) liên kết đến bảng `Shops`.
- **Amount**: Số tiền yêu cầu rút.
- **Status**: Trạng thái (`Pending`, `Approved`, `Rejected`).
- **BankAccountName**: Tên chủ tài khoản nhận.
- **BankAccountNumber**: Số tài khoản nhận.
- **BankName**: Tên ngân hàng nhận.
- **AdminNote**: Ghi chú từ admin.
- **CreatedAt**: Thời gian yêu cầu.
- **ProcessedAt**: Thời gian xử lý.
