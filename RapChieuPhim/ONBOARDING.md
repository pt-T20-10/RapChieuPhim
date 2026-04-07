# HƯỚNG DẪN ONBOARDING — Nhóm 1 RapChieuPhim

> **Đọc kỹ từ đầu đến cuối trước khi bắt đầu.**  
> Làm đúng thứ tự: Cài đặt → Database → Clone repo → Chạy thử → Bắt đầu code

---

## THÔNG TIN DỰ ÁN

| | |
|---|---|
| **Repo** | https://github.com/pt-T20-10/RapChieuPhim.git |
| **Tech Stack** | ASP.NET MVC .NET 8 + SQL Server + EF Core |
| **Lead** | Phan Trọng Thoại (225669) |

| MSSV | Họ tên | Nhánh làm việc |
|------|--------|----------------|
| 225669 | Phan Trọng Thoại | `feature/thanh-toan` |
| 223546 | Bùi Minh Nhựt | `feature/quet-ve` |
| 220996 | Nguyễn Hoàng Nghĩa | `feature/dat-ve` |
| 226056 | Phan Trung Nghĩa | `feature/thong-ke` |
| 226637 | Nguyễn Hoàng Lễ | `feature/quan-ly-danh-muc` |

---

## BƯỚC 1 — Cài đặt môi trường

### Phần mềm bắt buộc

| Phần mềm | Link tải | Lưu ý |
|----------|----------|-------|
| Visual Studio 2022 Community | https://visualstudio.microsoft.com | Chọn workload **ASP.NET and web development** |
| SQL Server 2019+ (bất kỳ edition) | https://www.microsoft.com/sql-server | Developer Edition miễn phí |
| SQL Server Management Studio (SSMS) | https://aka.ms/ssmsfullsetup | Dùng để chạy script SQL |
| Git | https://git-scm.com/downloads | Cài mặc định, next next finish |

### Kiểm tra sau khi cài

Mở **Command Prompt**, chạy từng lệnh — phải thấy version hiện ra:

```cmd
dotnet --version
git --version
```

---

## BƯỚC 2 — Setup Database

> Thực hiện trong **SQL Server Management Studio (SSMS)**

### 2.1 — Tải 2 file SQL từ repo

```
RapChieuPhim_Schema.sql   ← tạo database + bảng + index
RapChieuPhim_Data.sql     ← chèn dữ liệu mẫu
```

Vào repo GitHub → tìm 2 file trên → tải về máy.

### 2.2 — Chạy file Schema trước

1. Mở **SSMS** → kết nối vào SQL Server của máy
2. Nhấn **Open File** → chọn `RapChieuPhim_Schema.sql`
3. Nhấn **F5** hoặc **Execute**
4. Thấy thông báo `✅ RapChieuPhimDB tạo thành công!` là xong

### 2.3 — Chạy file Data sau

1. Nhấn **Open File** → chọn `RapChieuPhim_Data.sql`
2. Nhấn **F5** hoặc **Execute**
3. Thấy `✅ Seed data hoàn tất!` là xong

### 2.4 — Lấy Connection String của máy mình

Trong SSMS, chuột phải vào server → **Properties** → xem **Server name**.

Connection string thường có dạng:

```
# Nếu dùng Windows Authentication (phổ biến nhất)
Data Source=TEN_MAY\TEN_INSTANCE;Initial Catalog=RapChieuPhimDB;Integrated Security=True;Trust Server Certificate=True;

# Ví dụ thực tế
Data Source=ACER\MSSQLSERVER03;Initial Catalog=RapChieuPhimDB;Integrated Security=True;Trust Server Certificate=True;
Data Source=LAPTOP-ABC\SQLEXPRESS;Initial Catalog=RapChieuPhimDB;Integrated Security=True;Trust Server Certificate=True;

# Nếu dùng SQL Server Authentication
Data Source=TEN_MAY;Initial Catalog=RapChieuPhimDB;User Id=sa;Password=mat_khau;Trust Server Certificate=True;
```

**Ghi lại connection string này**, sẽ dùng ở Bước 4.

---

## BƯỚC 3 — Clone Repo và Checkout nhánh

Mở **Command Prompt** hoặc **Git Bash**, chạy lần lượt:

```bash
# Clone repo về máy
git clone https://github.com/pt-T20-10/RapChieuPhim.git

# Di chuyển vào thư mục project
cd RapChieuPhim

# Tải về tất cả nhánh từ remote
git fetch --all

# Checkout đúng nhánh của mình (xem bảng ở trên)
git checkout feature/ten-nhanh-cua-minh

# Ví dụ — Bùi Minh Nhựt:
git checkout feature/quet-ve
```

---

## BƯỚC 4 — Cấu hình Connection String

> **QUAN TRỌNG:** Không sửa `appsettings.json` trong repo.  
> Tạo file riêng cho máy của mình.

Trong thư mục `RapChieuPhim\RapChieuPhim\`, tạo file mới tên:

```
appsettings.Development.json
```

Nội dung file:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DÁN CONNECTION STRING CỦA MÁY MÌNH VÀO ĐÂY"
  }
}
```

Ví dụ thực tế:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=LAPTOP-ABC\\SQLEXPRESS;Initial Catalog=RapChieuPhimDB;Integrated Security=True;Trust Server Certificate=True;"
  }
}
```

> File này đã có trong `.gitignore` — sẽ **không bị commit lên repo**, thông tin máy mình chỉ ở local.

---

## BƯỚC 5 — Mở Project và Chạy thử

1. Mở file **`RapChieuPhim.sln`** bằng Visual Studio 2022
2. Chờ VS restore NuGet packages tự động (lần đầu mất 1-2 phút)
3. Kiểm tra góc dưới VS — thấy **Ready** là restore xong
4. Nhấn **Ctrl+F5** để chạy

Nếu thấy trang Home của ASP.NET hiện ra trên trình duyệt là **setup thành công**.

### Lỗi thường gặp

| Lỗi | Nguyên nhân | Cách fix |
|-----|-------------|----------|
| `Cannot connect to database` | Connection string sai | Kiểm tra lại Bước 4 |
| `NuGet restore failed` | Chưa có internet / chưa cấu hình nguồn | Vào Tools → NuGet → Package Sources → thêm `https://api.nuget.org/v3/index.json` |
| `Build failed` | Thiếu package | Chuột phải Solution → Restore NuGet Packages |
| Trang trắng / lỗi 500 | DB chưa có data | Chạy lại `RapChieuPhim_Data.sql` |

---

## BƯỚC 6 — Quy trình làm việc hàng ngày

### Buổi sáng — Trước khi code

```bash
# 1. Lấy code mới nhất từ develop
git checkout develop
git pull origin develop

# 2. Merge develop vào nhánh của mình
git checkout feature/ten-nhanh-cua-minh
git merge develop

# 3. Nếu có conflict → xử lý rồi commit
git add .
git commit -m "chore: merge develop vào nhánh"
```

### Trong ngày — Commit thường xuyên

```bash
git add .
git commit -m "feat: mô tả việc vừa làm"
git push origin feature/ten-nhanh-cua-minh
```

### Khi xong tính năng — Tạo Pull Request

1. Push lần cuối lên GitHub
2. Vào **GitHub → Pull Requests → New Pull Request**
3. Chọn:
   - **base:** `develop`
   - **compare:** `feature/ten-nhanh-cua-minh`
4. Điền title và mô tả những gì đã làm
5. Gán **Reviewer: pt-T20-10 (Thoại)**
6. Nhắn Zalo cho Lead biết để review

---

## QUY TẮC BẮT BUỘC

```
✅  Luôn làm việc trên nhánh feature của mình
✅  Commit message phải có type (feat/fix/style/chore...)
✅  Pull code từ develop MỖI NGÀY trước khi code
✅  Merge vào develop qua Pull Request, KHÔNG push thẳng

✗   Không push thẳng vào main hoặc develop
✗   Không dùng git push --force
✗   Không commit appsettings.Development.json
✗   Không commit thư mục bin/ obj/
```

---

## LIÊN HỆ KHI CẦN HỖ TRỢ

| Vấn đề | Liên hệ |
|--------|---------|
| Lỗi Git / conflict | Phan Trọng Thoại (Lead) |
| Lỗi Database | Phan Trọng Thoại (Lead) |
| Lỗi NuGet / build | Phan Trọng Thoại (Lead) |
| Hỏi về logic nghiệp vụ | Thảo luận trong nhóm Zalo |