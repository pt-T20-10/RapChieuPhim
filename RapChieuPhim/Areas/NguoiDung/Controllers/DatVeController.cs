using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Extensions; // Dùng lại thư viện Session Json
using RapChieuPhim.Models.Entities;

namespace RapChieuPhim.Areas.NguoiDung.Controllers
{
    [Area("NguoiDung")]
    public class DatVeController : Controller
    {
        private readonly AppDbContext _context;

        public DatVeController(AppDbContext context)
        {
            _context = context;
        }

        // 1. TRANG CHỌN SUẤT CHIẾU VÀ GHẾ
        [HttpGet]
        public async Task<IActionResult> ChonGhe(string maPhim)
        {
            HttpContext.Session.Remove("DatVe_GioHangBapNuocTam");

            // Truy vấn lấy thông tin phim
            var phim = await _context.Phim.FirstOrDefaultAsync(p => p.MaPhim == maPhim && p.DaXoa == false);
            if (phim == null) return NotFound("Không tìm thấy phim.");

            // Lấy danh sách suất chiếu của phim này từ hôm nay trở đi, gom nhóm theo Ngày
            var SuatChieu = await _context.SuatChieu
                .Include(s => s.MaPhongNavigation)
                .Where(s => s.MaPhim == maPhim && s.ThoiGianBatDau >= DateTime.Now && s.DaXoa == false)
                .OrderBy(s => s.ThoiGianBatDau)
                .ToListAsync();

            ViewBag.Phim = phim;
            ViewBag.SuatChieu = SuatChieu.GroupBy(s => s.ThoiGianBatDau.Date).ToList();

            return View();
        }

        // 2. API LẤY DANH SÁCH GHẾ (CHUẨN DATABASE)
        [HttpGet]
        public async Task<IActionResult> LaySoDoGhe(string maSuatChieu)
        {
            // Lấy suất chiếu kèm thông tin phòng
            var suatChieu = await _context.SuatChieu
                .Include(s => s.MaPhongNavigation)
                .FirstOrDefaultAsync(s => s.MaSuatChieu == maSuatChieu && s.DaXoa == false);

            if (suatChieu == null) return Json(new { success = false });

            // 1. Lấy TẤT CẢ ghế của phòng chiếu đó
            var dsGhe = await _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Where(g => g.MaPhong == suatChieu.MaPhong && g.DaXoa == false)
                .OrderBy(g => g.TenHang).ThenBy(g => g.SoThu) // Sắp xếp chuẩn A1 -> A2 -> B1
                .ToListAsync();

            // 2. Lấy TẤT CẢ vé đã bán/đang giữ của Suất Chiếu này
            // Cách này an toàn tuyệt đối: Dựa vào MaSuatChieu và trạng thái DonHang
            var veCuaSuatChieu = await _context.ChiTietVe
                .Include(v => v.MaDonHangNavigation)
                .Where(v => v.MaSuatChieu == maSuatChieu
                         && v.DaXoa == false
                         && v.MaDonHangNavigation.TrangThai != "DaHuy")
                .ToListAsync();

            // 3. Map dữ liệu để trả về cho Giao diện
            var result = dsGhe.Select(g => {
                int trangThaiGhe = 1; // 1: Trong (Trống)

                if (g.TrangThai == "DangBaoTri" || g.TrangThai == "DaKhoa")
                {
                    trangThaiGhe = 4; // 4: Đỏ (Bảo trì/Khóa)
                }
                else
                {
                    var ve = veCuaSuatChieu.FirstOrDefault(v => v.MaGhe == g.MaGhe);
                    if (ve != null)
                    {
                        if (ve.MaDonHangNavigation.TrangThai == "DaThanhToan" || ve.MaDonHangNavigation.TrangThai == "DaXuatHoaDon")
                            trangThaiGhe = 2; // 2: Xám (DaDat)
                        else if (ve.MaDonHangNavigation.TrangThai == "ChoThanhToan")
                        {
                            // KIỂM TRA ĐỒNG HỒ 5 PHÚT
                            if (ve.MaDonHangNavigation.NgayTao.AddMinutes(5) > DateTime.Now)
                            {
                                trangThaiGhe = 3; // 3: Vàng (DangGiu) - Vẫn còn trong 5 phút
                            }
                            else
                            {
                                trangThaiGhe = 1; // Quá 5 phút -> Trả lại màu Trắng (Trong)
                            }
                        }
                    }
                }

                return new
                {
                    MaGhe = g.MaGhe,
                    TenGhe = g.TenHang + g.SoThu,
                    GiaVeTong = suatChieu.GiaGoc * g.MaLoaiGheNavigation.HeSoGia,
                    LoaiGhe = g.MaLoaiGheNavigation.TenLoaiGhe,
                    TrangThai = trangThaiGhe
                };
            }).ToList();

            return Json(new { success = true, dsGhe = result, tenPhong = suatChieu.MaPhongNavigation.TenPhong });
        }

        // 3. TRANG XÁC NHẬN VÀ HIỂN THỊ ĐỒNG HỒ 5 PHÚT
        [HttpPost]
        public async Task<IActionResult> XacNhan(string maSuatChieu, List<string> selectedSeats)
        {
            if (string.IsNullOrEmpty(maSuatChieu) || selectedSeats == null || !selectedSeats.Any())
                return RedirectToAction("Index", "Home");

            // KIỂM TRA BẢO MẬT: Có ai nẫng tay trên ghế này trong tích tắc chưa?
            var veDaBan = await _context.ChiTietVe
                .Include(v => v.MaDonHangNavigation)
                .Where(v => v.MaSuatChieu == maSuatChieu && selectedSeats.Contains(v.MaGhe) && v.DaXoa == false)
                .ToListAsync();

            foreach (var ve in veDaBan)
            {
                var dh = ve.MaDonHangNavigation;
                if (dh.TrangThai == "DaThanhToan" || (dh.TrangThai == "ChoThanhToan" && dh.NgayTao.AddMinutes(5) > DateTime.Now))
                {
                    // Ghế đã bị giữ, báo lỗi và văng về trang chọn
                    return Content("<script>alert('Rất tiếc, ghế bạn chọn vừa có người đặt. Vui lòng chọn ghế khác!'); window.history.back();</script>", "text/html");
                }
            }

            // TẠO ĐƠN HÀNG "NHÁP" ĐỂ GIỮ GHẾ
            long orderCode = long.Parse(DateTime.Now.ToString("yyMMddHHmmss"));
            string maKhachHang = HttpContext.Session.GetString("MaKhachHang");

            var suatChieu = await _context.SuatChieu.Include(s => s.MaPhimNavigation).Include(s => s.MaPhongNavigation).FirstOrDefaultAsync(s => s.MaSuatChieu == maSuatChieu);
            var Ghe = await _context.Ghe.Include(g => g.MaLoaiGheNavigation).Where(g => selectedSeats.Contains(g.MaGhe)).ToListAsync();
            double tongTienVe = Ghe.Sum(g => suatChieu.GiaGoc * g.MaLoaiGheNavigation.HeSoGia);

            var donHang = new DonHang
            {
                MaDonHang = orderCode.ToString(),
                MaKhachHang = string.IsNullOrEmpty(maKhachHang) ? null : maKhachHang,
                NgayTao = DateTime.Now,
                TongTienBanDau = tongTienVe,
                TongTienSauGiam = tongTienVe,
                TrangThai = "ChoThanhToan", // Bắt đầu tính giờ giữ ghế
                DaXoa = false
            };
            _context.DonHang.Add(donHang);

            foreach (var g in Ghe)
            {
                var ct = new ChiTietVe
                {
                    MaVe = "V" + DateTime.Now.Ticks.ToString().Substring(10),
                    MaDonHang = donHang.MaDonHang,
                    MaSuatChieu = maSuatChieu,
                    MaGhe = g.MaGhe,
                    GiaVe = suatChieu.GiaGoc * g.MaLoaiGheNavigation.HeSoGia,
                    TrangThai = "ChoXuLy",
                    DaXoa = false
                };
                _context.ChiTietVe.Add(ct);
            }
            await _context.SaveChangesAsync();

            // Lưu MaDonHang này vào Session để lát chuyển qua ThanhToanController cập nhật tiếp (bắp nước) thay vì tạo mới
            HttpContext.Session.SetString("DatVe_MaDonHang_Tam", donHang.MaDonHang);

            // ... (Đoạn lấy MenuBapNuoc và Viewbags trả về View XacNhan giữ nguyên như cũ)
            var menuBapNuoc = await _context.DanhMucDichVu.Include(dm => dm.DichVu.Where(dv => dv.DaXoa == false)).Where(dm => dm.DaXoa == false).ToListAsync();
            ViewBag.SuatChieu = suatChieu; ViewBag.Ghe = Ghe; ViewBag.MenuBapNuoc = menuBapNuoc; ViewBag.TongTienVe = tongTienVe;
            ViewBag.ExpireTime = donHang.NgayTao.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss");

            ViewBag.JsonBapNuoc = HttpContext.Session.GetString("DatVe_GioHangBapNuocTam") ?? "[]";

            return View();
        }

        // 4. API LƯU DỮ LIỆU ĐỂ SANG TRANG THANH TOÁN (PAYOS)
        [HttpPost]
        public IActionResult LuuSessionThanhToan(string maSuatChieu, List<string> maGhe, string jsonBapNuoc)
        {
            // Lưu trạng thái Đặt Vé
            HttpContext.Session.SetString("DatVe_MaSuatChieu", maSuatChieu);
            HttpContext.Session.Set("DatVe_MaGhe", maGhe);
            HttpContext.Session.SetString("LoaiGiaoDich", "DatVe"); // Phân biệt với khách chỉ mua bắp nước lẻ

            // Xử lý giỏ bắp nước (nếu có mua thêm)
            var bapNuocs = string.IsNullOrEmpty(jsonBapNuoc)
                ? new List<RapChieuPhim.Models.ViewModels.CartItem>()
                : System.Text.Json.JsonSerializer.Deserialize<List<RapChieuPhim.Models.ViewModels.CartItem>>(jsonBapNuoc);

            HttpContext.Session.Set("GioHangBapNuoc", bapNuocs);

            // Trả về link để Javascript tự redirect
            return Json(new { success = true, redirectUrl = Url.Action("Index", "ThanhToan", new { area = "NguoiDung" }) });
        }

        // 5. TIẾP TỤC ĐẶT VÉ (Load lại trang xác nhận cũ)
        [HttpGet]
        public async Task<IActionResult> TiepTucDatVe(string maDonHang)
        {
            var donHang = await _context.DonHang
                .Include(d => d.ChiTietVe)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.TrangThai == "ChoThanhToan");

            // Nếu đơn hàng không tồn tại hoặc đã lố 5 phút
            if (donHang == null || donHang.NgayTao.AddMinutes(5) <= DateTime.Now)
            {
                if (donHang != null)
                {
                    donHang.TrangThai = "DaHuy";
                    var ChiTietVe = await _context.ChiTietVe.Where(v => v.MaDonHang == maDonHang).ToListAsync();
                    foreach (var ve in ChiTietVe) ve.TrangThai = "DaHuy";
                    await _context.SaveChangesAsync();
                }
                HttpContext.Session.Remove("DatVe_MaDonHang_Tam");
                return Content("<script>alert('Giao dịch đã hết hạn do quá 5 phút!'); window.location.href='/';</script>", "text/html");
            }

            // Phục hồi lại dữ liệu cho View XacNhan.cshtml
            var maSuatChieu = donHang.ChiTietVe.First().MaSuatChieu;
            var selectedSeatIds = donHang.ChiTietVe.Select(v => v.MaGhe).ToList();

            var suatChieu = await _context.SuatChieu.Include(s => s.MaPhimNavigation).Include(s => s.MaPhongNavigation).FirstOrDefaultAsync(s => s.MaSuatChieu == maSuatChieu);
            var Ghe = await _context.Ghe.Include(g => g.MaLoaiGheNavigation).Where(g => selectedSeatIds.Contains(g.MaGhe)).ToListAsync();
            var menuBapNuoc = await _context.DanhMucDichVu.Include(dm => dm.DichVu.Where(dv => dv.DaXoa == false)).Where(dm => dm.DaXoa == false).ToListAsync();

            ViewBag.SuatChieu = suatChieu;
            ViewBag.Ghe = Ghe;
            ViewBag.MenuBapNuoc = menuBapNuoc;
            ViewBag.TongTienVe = donHang.TongTienBanDau;

            // Tính toán lại thời gian hết hạn chính xác từ lúc tạo đơn
            ViewBag.ExpireTime = donHang.NgayTao.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss");
            ViewBag.JsonBapNuoc = HttpContext.Session.GetString("DatVe_GioHangBapNuocTam") ?? "[]";

            return View("XacNhan"); // Tái sử dụng View cũ
        }

        // 6. HỦY ĐƠN HÀNG TẠM, TRẢ LẠI GHẾ TRỐNG
        [HttpPost]
        public async Task<IActionResult> HuyDonHangTam(string maDonHang)
        {
            var donHang = await _context.DonHang.FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);
            if (donHang != null && donHang.TrangThai == "ChoThanhToan")
            {
                donHang.TrangThai = "DaHuy"; // Chuyển Hủy

                // Trả các vé về Hủy để nhả ghế ra sơ đồ
                var ChiTietVe = await _context.ChiTietVe.Where(v => v.MaDonHang == maDonHang).ToListAsync();
                foreach (var ve in ChiTietVe) ve.TrangThai = "DaHuy";

                await _context.SaveChangesAsync();
            }
            HttpContext.Session.Remove("DatVe_MaDonHang_Tam");
            HttpContext.Session.Remove("DatVe_GioHangBapNuocTam");
            return Json(new { success = true });
        }

        // API LƯU TẠM BẮP NƯỚC VÀO SESSION KHI ĐANG CHỌN
        [HttpPost]
        public IActionResult SyncBapNuocTam(string jsonBapNuoc)
        {
            HttpContext.Session.SetString("DatVe_GioHangBapNuocTam", jsonBapNuoc ?? "[]");
            return Json(new { success = true });
        }
    }
}