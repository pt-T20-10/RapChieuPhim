# HƯỚNG DẪN ONBOARDING — Nhóm 1 RapChieuPhim

> **Đọc kỹ từ đầu đến cuối trước khi bắt đầu.**
> Làm đúng thứ tự: Cài đặt → Database → Clone repo → Chạy thử → Bắt đầu code

---

## THÔNG TIN DỰ ÁN

| | |
|---|---|
| **Repo** | https://github.com/pt-T20-10/RapChieuPhim.git |
| **Tech Stack** | ASP.NET MVC .NET 8 + SQL Server + EF Core 8 |
| **Lead** | Phan Trọng Thoại (225669) |

| MSSV | Họ tên | Nhánh làm việc | Module |
|------|--------|----------------|--------|
| 225669 | Phan Trọng Thoại | `feature/thanh-toan` | Lead + Thanh toán + Bán hàng |
| 223546 | Bùi Minh Nhựt | `feature/quet-ve` | Quét mã vé |
| 220996 | Nguyễn Hoàng Nghĩa | `feature/dat-ve` | Đặt vé + Xác thực |
| 226056 | Phan Trung Nghĩa | `feature/thong-ke` | Thống kê + Báo cáo |
| 226637 | Nguyễn Hoàng Lễ | `feature/quan-ly-danh-muc` | Quản lý danh mục |

---

## BƯỚC 1 — Cài đặt môi trường

### Phần mềm bắt buộc

| Phần mềm | Lưu ý |
|----------|-------|
| Visual Studio 2022 Community | Chọn workload **ASP.NET and web development** |
| SQL Server 2019+ (Developer Edition — miễn phí) | |
| SQL Server Management Studio (SSMS) | Để chạy script SQL |
| Git | Cài mặc định |

### Kiểm tra sau khi cài — mở CMD chạy:

```cmd
dotnet --version   → phải thấy 8.x.x
git --version      → phải thấy 2.x.x
```

---

## BƯỚC 2 — Setup Database

> Thực hiện trong **SSMS**

### 2.1 — Tải 2 file SQL từ repo

```
Schema.sql   ← tạo database + 15 bảng + 7 index
Data.sql     ← chèn dữ liệu mẫu
```

### 2.2 — Chạy Schema.sql trước

1. Mở SSMS → kết nối SQL Server
2. **File → Open → Schema.sql** → nhấn **F5**
3. Thấy `✅ Schema tạo thành công!` là xong

### 2.3 — Chạy Data.sql sau

1. **File → Open → Data.sql** → nhấn **F5**
2. Thấy `✅ Seed data hoàn tất!` là xong

### 2.4 — Lấy Connection String máy mình

```
Data Source=TEN_MAY\TEN_INSTANCE;Initial Catalog=RapChieuPhimDB;Integrated Security=True;Trust Server Certificate=True;
```

**Ghi lại**, dùng ở Bước 4.

---

## BƯỚC 3 — Clone repo và checkout nhánh

```bash
git clone https://github.com/pt-T20-10/RapChieuPhim.git
cd RapChieuPhim
git fetch --all
git checkout feature/ten-nhanh-cua-minh
```

---

## BƯỚC 4 — Cấu hình Connection String

> **KHÔNG sửa `appsettings.json`** — tạo file riêng cho máy mình.

Tạo file `appsettings.Development.json` trong thư mục `RapChieuPhim/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DÁN CONNECTION STRING CỦA MÁY MÌNH VÀO ĐÂY"
  }
}
```

File này đã có trong `.gitignore` — sẽ không bị commit lên repo.

---

## BƯỚC 5 — Mở project và chạy thử

1. Mở **`RapChieuPhim.sln`** bằng Visual Studio 2022
2. Chờ restore NuGet tự động (lần đầu 1-2 phút)
3. Nhấn **Ctrl+F5**

### Kết quả mong đợi

```
https://localhost:xxxx/          → Giao diện Người dùng
https://localhost:xxxx/RapPhim   → Dashboard quản lý Rạp phim
```

---

## CẤU TRÚC DỰ ÁN — ĐỌC KỸ TRƯỚC KHI CODE

```
RapChieuPhim/
│
├── Areas/
│   ├── NguoiDung/                    ← giao diện web khách hàng
│   │   ├── Controllers/
│   │   │   ├── HomeController.cs
│   │   │   ├── DatVeController.cs
│   │   │   ├── ThanhToanController.cs
│   │   │   └── TaiKhoanController.cs
│   │   └── Views/
│   │       ├── _ViewImports.cshtml
│   │       ├── _ViewStart.cshtml     ← trỏ tới _LayoutNguoiDung
│   │       ├── Shared/
│   │       │   └── _LayoutNguoiDung.cshtml
│   │       ├── Home/
│   │       ├── DatVe/
│   │       ├── ThanhToan/
│   │       └── TaiKhoan/
│   │
│   └── RapPhim/                      ← giao diện quản lý + nghiệp vụ
│       ├── Controllers/
│       │   ├── DashboardController.cs
│       │   ├── PhimController.cs
│       │   ├── SuatChieuController.cs
│       │   ├── PhongChieuController.cs
│       │   ├── KhuyenMaiController.cs
│       │   ├── NhanVienController.cs
│       │   ├── BanHangController.cs
│       │   ├── QuetVeController.cs
│       │   └── ThongKeController.cs
│       └── Views/
│           ├── _ViewImports.cshtml
│           ├── _ViewStart.cshtml     ← trỏ tới _LayoutRapPhim
│           ├── Shared/
│           │   └── _LayoutRapPhim.cshtml
│           ├── Dashboard/            ← trang chào khi vào RapPhim
│           ├── Phim/                 ← Index, Create, Edit, Delete, Details
│           ├── SuatChieu/
│           ├── PhongChieu/
│           ├── KhuyenMai/
│           ├── NhanVien/
│           ├── BanHang/
│           ├── QuetVe/
│           └── ThongKe/
│
├── Models/
│   ├── Entities/                     ← EF SCAFFOLD SINH RA — không sửa tay
│   └── ViewModels/                   ← viết tay, dùng chung 2 Area
│       ├── VeViewModel.cs
│       ├── HoaDonViewModel.cs
│       └── ...
│
├── Services/                         ← business logic, dùng chung 2 Area
│
├── Data/
│   └── AppDbContext.cs               ← EF SCAFFOLD SINH RA — không sửa tay
│
└── Views/
    └── Shared/
        ├── _TicketPartial.cshtml     ← partial vé dùng chung 2 Area
        └── _InvoicePartial.cshtml    ← partial hóa đơn dùng chung 2 Area
```

---

## QUY TẮC CODE BẮT BUỘC

### 1. Controller phải có `[Area]` và đúng namespace

```csharp
// Area NguoiDung
namespace RapChieuPhim.Areas.NguoiDung.Controllers
{
    [Area("NguoiDung")]
    public class DatVeController : Controller { }
}

// Area RapPhim
namespace RapChieuPhim.Areas.RapPhim.Controllers
{
    [Area("RapPhim")]
    public class PhimController : Controller { }
}
```

### 2. ViewModel đúng namespace

```csharp
namespace RapChieuPhim.Models.ViewModels
{
    public class DatVeViewModel { }
}
```

### 3. Service đúng namespace

```csharp
namespace RapChieuPhim.Services
{
    public class DatVeService { }
}
```

### 4. View KHÔNG hardcode Layout

```cshtml
@* ❌ SAI *@
@{ Layout = "~/Views/Shared/_Layout.cshtml"; }

@* ✅ ĐÚNG — để _ViewStart.cshtml tự xử lý *@
@{ ViewData["Title"] = "Tên trang"; }
```

### 5. Không sửa `Models/Entities/` và `Data/AppDbContext.cs`

Các file này do EF Scaffold sinh ra. Nếu DB thay đổi thì báo Lead — Lead chạy Scaffold lại, commit, team pull về.

### 6. Gọi đúng Area trong Tag Helper

```cshtml
@* Link trong NguoiDung Area *@
<a asp-area="NguoiDung" asp-controller="DatVe" asp-action="Index">Đặt vé</a>

@* Link trong RapPhim Area *@
<a asp-area="RapPhim" asp-controller="Phim" asp-action="Index">Quản lý phim</a>

@* Chuyển qua Area khác *@
<a asp-area="RapPhim" asp-controller="Dashboard" asp-action="Index">⚙️ Vào quản lý</a>
<a asp-area="NguoiDung" asp-controller="Home" asp-action="Index">🌐 Về trang chủ</a>
```

### 7. Include đủ navigation khi query EF

```csharp
// ✅ ĐÚNG
var donHang = await _context.DonHangs
    .Include(d => d.MaKhachHangNavigation)
    .Include(d => d.ChiTietVes)
        .ThenInclude(v => v.MaSuatChieuNavigation)
            .ThenInclude(s => s.MaPhimNavigation)
    .FirstOrDefaultAsync(d => d.MaDonHang == id);

// ❌ SAI — navigation sẽ null, gây NullReferenceException
var donHang = await _context.DonHangs.FindAsync(id);
```

### 8. Dùng Partial View cho vé và hóa đơn

```cshtml
@* Vé xem phim — model là VeViewModel *@
<partial name="~/Views/Shared/_TicketPartial.cshtml" model="veViewModel" />

@* Hóa đơn — model là HoaDonViewModel *@
<partial name="~/Views/Shared/_InvoicePartial.cshtml" model="hoaDonViewModel" />
```

---

## PHÂN CÔNG CHI TIẾT

| Thành viên | Làm việc trong | Controller | Views tương ứng |
|---|---|---|---|
| Thoại (Lead) | Areas/RapPhim | BanHangController | BanHang/ |
| Thoại (Lead) | Areas/NguoiDung | ThanhToanController | ThanhToan/ |
| Nhựt | Areas/RapPhim | QuetVeController | QuetVe/ |
| Nghĩa | Areas/NguoiDung | DatVeController, TaiKhoanController | DatVe/, TaiKhoan/ |
| Trung Nghĩa | Areas/RapPhim | ThongKeController | ThongKe/ |
| Lễ | Areas/RapPhim | Phim/SuatChieu/PhongChieu/KhuyenMai/NhanVienController | Views tương ứng |

> Controllers CRUD (Index/Create/Edit/Delete) cho Phim, SuatChieu, PhongChieu, KhuyenMai, NhanVien đã được scaffold sẵn. Lễ chỉ cần thêm logic nghiệp vụ và hoàn thiện Views.

---

## QUY TRÌNH GIT HÀNG NGÀY

### Buổi sáng — trước khi code

```bash
git checkout develop
git pull origin develop
git checkout feature/ten-nhanh-cua-minh
git merge develop
```

### Trong ngày — commit thường xuyên

```bash
git add .
git commit -m "feat: mô tả việc vừa làm"
git push origin feature/ten-nhanh-cua-minh
```

### Khi xong tính năng — tạo Pull Request

1. Push lần cuối lên GitHub
2. **GitHub → Pull Requests → New Pull Request**
3. base: `develop` ← compare: `feature/ten-nhanh`
4. Gán Reviewer: **pt-T20-10 (Thoại)**
5. Nhắn Zalo cho Lead

### Convention commit message

```
feat:     thêm tính năng mới
fix:      sửa bug
style:    sửa UI / View
refactor: cải thiện code
chore:    cấu hình, setup
docs:     cập nhật tài liệu
```

---

## LỖI THƯỜNG GẶP

| Lỗi | Nguyên nhân | Cách fix |
|-----|-------------|----------|
| `The view 'Index' was not found` | View chưa tạo hoặc sai thư mục | Kiểm tra file trong `Areas/TenArea/Views/TenController/` |
| `NullReferenceException` navigation | Chưa `.Include()` khi query | Thêm đủ `.Include().ThenInclude()` |
| Route 404 | Thiếu `[Area]` hoặc sai namespace | Kiểm tra attribute và namespace controller |
| Layout null / trang trắng | View hardcode `Layout = null` | Xóa dòng đó |
| `Cannot connect to database` | Connection string sai | Kiểm tra `appsettings.Development.json` |
| Build failed sau khi pull | Entities hoặc ViewModels thay đổi | Rebuild Solution, kiểm tra namespace |
| Tag Helper sinh URL sai | Thiếu `asp-area` | Luôn khai báo đủ `asp-area`, `asp-controller`, `asp-action` |

---

## LIÊN HỆ KHI CẦN HỖ TRỢ

| Vấn đề | Liên hệ |
|--------|---------|
| Lỗi Git / conflict | Phan Trọng Thoại (Lead) |
| DB thay đổi / Schema mới | Lead chạy Scaffold → push → team pull |
| Lỗi build / NuGet | Phan Trọng Thoại |
| Hỏi về logic nghiệp vụ | Thảo luận nhóm Zalo |