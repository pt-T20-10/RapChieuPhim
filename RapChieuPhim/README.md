@'
# Hệ thống Quản lý Rạp chiếu phim

## Tech Stack
- ASP.NET MVC .NET 8
- Entity Framework Core 8
- SQL Server
- Hangfire (Background Jobs)

## Thành viên nhóm 1
| MSSV | Họ tên | Module |
|------|--------|--------|
| 225669 | Phan Trọng Thoại | Lead + Thanh toán + Bán hàng trực tiếp |
| 223546 | Bùi Minh Nhựt | Quét mã vé |
| 220996 | Nguyễn Hoàng Nghĩa | Đặt vé + Xác thực |
| 226056 | Phan Trung Nghĩa | Thống kê + báo cáo |
| 226637 | Nguyễn Hoàng Lễ | Quản lý danh mục + Authorization |

## Setup

### 1. Database
Chạy file `RapChieuPhim_Database.sql` trên SQL Server

### 2. Connection String
Mở `appsettings.json`, sửa connection string phù hợp máy local:
```json
"DefaultConnection": "Data Source=TEN_MAY\\TEN_INSTANCE;Initial Catalog=RapChieuPhimDB;Integrated Security=True;Trust Server Certificate=True;"
```

### 3. Chạy project