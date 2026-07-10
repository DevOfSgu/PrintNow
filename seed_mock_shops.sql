-- SQL Script: Seed Mock Print Shops & Services for PrintNowDb
-- Execute this script on your SQL Server Database (PrintNowDb)

USE [PrintNowDb];
GO

-- 1. Insert Shop Owners into Users Table
-- Passwords are kept in plain text as per AuthController setup
INSERT INTO [Users] ([FullName], [Phone], [Email], [PasswordHash], [Role], [CreatedAt])
VALUES 
(N'Nguyễn Văn Fast', '0912345678', 'fastprint@gmail.com', '123456', 'ShopOwner', GETDATE()),
(N'Trần Bách Khoa', '0987654321', 'bachkhoaprint@gmail.com', '123456', 'ShopOwner', GETDATE()),
(N'Lê Sinh Viên', '0905556667', 'sinhvienprint@gmail.com', '123456', 'ShopOwner', GETDATE());
GO

-- 2. Insert Shops and link to their respective Owner (retrieve generated IDs)
DECLARE @Owner1 INT, @Owner2 INT, @Owner3 INT;

SELECT @Owner1 = [Id] FROM [Users] WHERE [Email] = 'fastprint@gmail.com';
SELECT @Owner2 = [Id] FROM [Users] WHERE [Email] = 'bachkhoaprint@gmail.com';
SELECT @Owner3 = [Id] FROM [Users] WHERE [Email] = 'sinhvienprint@gmail.com';

INSERT INTO [Shops] (
    [OwnerId], [ShopName], [AddressText], [Latitude], [Longitude], 
    [ImageUrl], [IsActive], [Description], [OperatingHoursText], 
    [BankAccountName], [BankAccountNumber], [BankName], [IsLocked], [Balance], [CreatedAt]
)
VALUES
(
    @Owner1, N'Tiệm In Nhanh FastPrint', N'123 Nguyễn Văn Cừ, Quận 5, TP. HCM', 
    10.7624, 106.6822, 'https://images.unsplash.com/photo-1563986768609-322da13575f3?w=500', 
    1, N'Chuyên in nhanh tài liệu, đồ án tốt nghiệp lấy ngay.', N'07:00 - 21:00',
    N'NGUYEN VAN FAST', '1234567890123', N'Vietcombank', 0, 1500000.00, GETDATE()
),
(
    @Owner2, N'In Ấn Bách Khoa', N'268 Lý Thường Kiệt, Quận 10, TP. HCM', 
    10.7728, 106.6599, 'https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=500', 
    1, N'In ấn giá rẻ, photocopy số lượng lớn cho sinh viên.', N'08:00 - 18:00',
    N'TRAN BACH KHOA', '9876543210987', N'Techcombank', 0, 850000.00, GETDATE()
),
(
    @Owner3, N'In Sinh Viên Giá Rẻ', N'Khu phố 6, Linh Trung, Thủ Đức, TP. HCM', 
    10.8700, 106.8028, 'https://images.unsplash.com/photo-1506784983877-45594efa4cbe?w=500', 
    1, N'Tiệm in nằm ngay ký túc xá Đại học Quốc Gia, phục vụ 24/7.', N'07:00 - 22:30',
    N'LE SINH VIEN', '102030405060', N'MB Bank', 0, 0.00, GETDATE()
);
GO

-- 3. Insert core services for the newly created shops
DECLARE @Shop1 INT, @Shop2 INT, @Shop3 INT;

SELECT @Shop1 = [Id] FROM [Shops] WHERE [ShopName] = N'Tiệm In Nhanh FastPrint';
SELECT @Shop2 = [Id] FROM [Shops] WHERE [ShopName] = N'In Ấn Bách Khoa';
SELECT @Shop3 = [Id] FROM [Shops] WHERE [ShopName] = N'In Sinh Viên Giá Rẻ';

INSERT INTO [Services] ([ShopId], [ServiceCode], [ServiceName], [BasePrice], [Unit], [IsActive], [Type])
VALUES
-- FastPrint Services
(@Shop1, 'A4_BW', N'In A4 Trắng Đen', 500.00, N'trang', 1, 'Core'),
(@Shop1, 'A4_COLOR', N'In A4 Màu', 2000.00, N'trang', 1, 'Core'),
(@Shop1, 'A3_BW', N'In A3 Trắng Đen', 1200.00, N'trang', 1, 'Core'),
(@Shop1, 'BIND_SPIN', N'Đóng gáy xoắn', 15000.00, N'cuốn', 1, 'AddOn'),
(@Shop1, 'COVER_COLOR', N'Bìa kiếng màu', 5000.00, N'cuốn', 1, 'AddOn'),

-- Bách Khoa Services
(@Shop2, 'A4_BW', N'In A4 Trắng Đen', 400.00, N'trang', 1, 'Core'),
(@Shop2, 'A4_COLOR', N'In A4 Màu', 1500.00, N'trang', 1, 'Core'),
(@Shop2, 'BIND_GLUE', N'Đóng gáy keo nhiệt', 20000.00, N'cuốn', 1, 'AddOn'),
(@Shop2, 'COVER_COLOR', N'Bìa kiếng màu', 4500.00, N'cuốn', 1, 'AddOn'),

-- Sinh Viên Services
(@Shop3, 'A4_BW', N'In A4 Trắng Đen', 350.00, N'trang', 1, 'Core'),
(@Shop3, 'A4_COLOR', N'In A4 Màu', 1200.00, N'trang', 1, 'Core'),
(@Shop3, 'BIND_SPIN', N'Đóng gáy xoắn', 10000.00, N'cuốn', 1, 'AddOn');
GO

PRINT 'Mock data seeded successfully!';
