# Hướng dẫn chạy Ngrok cho Localhost (ASP.NET Core)

## 1. Tải và cài đặt Ngrok
Nếu máy bạn chưa có ngrok, hãy tải tại: [https://ngrok.com/download](https://ngrok.com/download)
Sau khi tải và giải nén, thêm authtoken của bạn:
```powershell
ngrok config add-authtoken <YOUR_AUTH_TOKEN>
```

## 2. Chạy ứng dụng
Mở terminal trong thư mục `PrintNow.Web` và chạy:
```powershell
dotnet run
```
Ghi chú lại cổng (port) đang chạy (thường là `5195` cho HTTP hoặc `7101` cho HTTPS).

## 3. Chạy Ngrok
Mở một terminal khác và chạy lệnh sau (thay `5195` bằng port ứng dụng của bạn):
```powershell
ngrok http 5195
```
*(Nếu bạn muốn forward HTTPS, chạy `ngrok http https://localhost:7101`)*

## 4. Sử dụng
Ngrok sẽ cung cấp một Forwarding URL dạng: `https://<random>.ngrok-free.app`.
Hãy gửi URL này vào điện thoại của bạn, đăng nhập tài khoản khách hàng. Trình duyệt trên điện thoại sẽ xin cấp quyền truy cập Vị trí (GPS) và sẽ hiển thị chính xác vị trí của bạn trên bản đồ thay vì bị chặn (như khi test localhost không có HTTPS trên điện thoại).
