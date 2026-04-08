using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Extensions;
using RapChieuPhim.Models.Entities;
using RapChieuPhim.Services;

namespace RapChieuPhim.Areas.NguoiDung.Controllers
{
    [Area("NguoiDung")]
    public class DatVeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly DatVeService _datVeService;
        public DatVeController(AppDbContext context, DatVeService datVeService)
        {
            _context = context;
            _datVeService = datVeService;
        }

        [HttpGet]
        public async Task<IActionResult> ChiTietPhim(string maPhim)
        {
            if (string.IsNullOrEmpty(maPhim)) return RedirectToAction("Index", "Home");

            var phim = await _context.Phim
                .Include(p => p.MaTheLoaiNavigation)
                .FirstOrDefaultAsync(p => p.MaPhim == maPhim && p.DaXoa == false);

            if (phim == null) return NotFound("Phim này không tồn tại hoặc đã ngừng chiếu.");

            return View(phim);
        }

        [HttpGet]
        public async Task<IActionResult> ChonGhe(string maPhim)
        {
            HttpContext.Session.Remove("DatVe_GioHangBapNuocTam");

            var phim = await _context.Phim.FirstOrDefaultAsync(p => p.MaPhim == maPhim && p.DaXoa == false);
            if (phim == null) return NotFound("Không tìm thấy phim.");

            var SuatChieu = await _context.SuatChieu
                .Include(s => s.MaPhongNavigation)
                .Where(s => s.MaPhim == maPhim && s.ThoiGianBatDau >= DateTime.Now && s.DaXoa == false)
                .OrderBy(s => s.ThoiGianBatDau)
                .ToListAsync();

            ViewBag.Phim = phim;
            ViewBag.SuatChieu = SuatChieu.GroupBy(s => s.ThoiGianBatDau.Date).ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LaySoDoGhe(string maSuatChieu)
        {
            var suatChieu = await _context.SuatChieu
                .Include(s => s.MaPhongNavigation)
                .FirstOrDefaultAsync(s => s.MaSuatChieu == maSuatChieu && s.DaXoa == false);

            if (suatChieu == null) return Json(new { success = false });

            var dsGhe = await _context.Ghe
                .Include(g => g.MaLoaiGheNavigation)
                .Where(g => g.MaPhong == suatChieu.MaPhong && g.DaXoa == false)
                .OrderBy(g => g.TenHang).ThenBy(g => g.SoThu)
                .ToListAsync();

            var veCuaSuatChieu = await _context.ChiTietVe
                .Include(v => v.MaDonHangNavigation)
                .Where(v => v.MaSuatChieu == maSuatChieu
                         && v.DaXoa == false
                         && v.MaDonHangNavigation.TrangThai != "DaHuy")
                .ToListAsync();

            var result = dsGhe.Select(g => {
                int trangThaiGhe = 1;

                if (g.TrangThai == "DangBaoTri" || g.TrangThai == "DaKhoa")
                {
                    trangThaiGhe = 4;
                }
                else
                {
                    var ve = veCuaSuatChieu.FirstOrDefault(v => v.MaGhe == g.MaGhe);
                    if (ve != null)
                    {
                        if (ve.MaDonHangNavigation.TrangThai == "DaThanhToan" || ve.MaDonHangNavigation.TrangThai == "DaXuatHoaDon")
                            trangThaiGhe = 2;
                        else if (ve.MaDonHangNavigation.TrangThai == "ChoThanhToan")
                        {
                            if (ve.MaDonHangNavigation.NgayTao.AddMinutes(5) > DateTime.Now)
                            {
                                trangThaiGhe = 3;
                            }
                            else
                            {
                                trangThaiGhe = 1;
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

        [HttpPost]
        public async Task<IActionResult> XacNhan(string maSuatChieu, List<string> selectedSeats)
        {
            if (string.IsNullOrEmpty(maSuatChieu) || selectedSeats == null || !selectedSeats.Any())
                return RedirectToAction("Index", "Home");

            var veDaBan = await _context.ChiTietVe
                .Include(v => v.MaDonHangNavigation)
                .Where(v => v.MaSuatChieu == maSuatChieu && selectedSeats.Contains(v.MaGhe) && v.DaXoa == false)
                .ToListAsync();

            foreach (var ve in veDaBan)
            {
                var dh = ve.MaDonHangNavigation;
                if (dh.TrangThai == "DaThanhToan" || (dh.TrangThai == "ChoThanhToan" && dh.NgayTao.AddMinutes(5) > DateTime.Now))
                {
                    return Content("<script>alert('Rất tiếc, ghế bạn chọn vừa có người đặt. Vui lòng chọn ghế khác!'); window.history.back();</script>", "text/html");
                }
            }

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
                TrangThai = "ChoThanhToan",
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

            HttpContext.Session.SetString("DatVe_MaDonHang_Tam", donHang.MaDonHang);

            var menuBapNuoc = await _context.DanhMucDichVu.Include(dm => dm.DichVu.Where(dv => dv.DaXoa == false)).Where(dm => dm.DaXoa == false).ToListAsync();
            ViewBag.SuatChieu = suatChieu; ViewBag.Ghe = Ghe; ViewBag.MenuBapNuoc = menuBapNuoc; ViewBag.TongTienVe = tongTienVe;
            ViewBag.ExpireTime = donHang.NgayTao.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss");

            ViewBag.JsonBapNuoc = HttpContext.Session.GetString("DatVe_GioHangBapNuocTam") ?? "[]";

            return View();
        }

        [HttpPost]
        public IActionResult LuuSessionThanhToan(string maSuatChieu, List<string> maGhe, string jsonBapNuoc)
        {
            HttpContext.Session.SetString("DatVe_MaSuatChieu", maSuatChieu);
            HttpContext.Session.Set("DatVe_MaGhe", maGhe);
            HttpContext.Session.SetString("LoaiGiaoDich", "DatVe");

            var bapNuocs = string.IsNullOrEmpty(jsonBapNuoc)
                ? new List<RapChieuPhim.Models.ViewModels.CartItem>()
                : System.Text.Json.JsonSerializer.Deserialize<List<RapChieuPhim.Models.ViewModels.CartItem>>(jsonBapNuoc);

            HttpContext.Session.Set("GioHangBapNuoc", bapNuocs);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "ThanhToan", new { area = "NguoiDung" }) });
        }

        [HttpGet]
        public async Task<IActionResult> TiepTucDatVe(string maDonHang)
        {
            var donHang = await _context.DonHang
                .Include(d => d.ChiTietVe)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.TrangThai == "ChoThanhToan");

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

            var maSuatChieu = donHang.ChiTietVe.First().MaSuatChieu;
            var selectedSeatIds = donHang.ChiTietVe.Select(v => v.MaGhe).ToList();

            var suatChieu = await _context.SuatChieu.Include(s => s.MaPhimNavigation).Include(s => s.MaPhongNavigation).FirstOrDefaultAsync(s => s.MaSuatChieu == maSuatChieu);
            var Ghe = await _context.Ghe.Include(g => g.MaLoaiGheNavigation).Where(g => selectedSeatIds.Contains(g.MaGhe)).ToListAsync();
            var menuBapNuoc = await _context.DanhMucDichVu.Include(dm => dm.DichVu.Where(dv => dv.DaXoa == false)).Where(dm => dm.DaXoa == false).ToListAsync();

            ViewBag.SuatChieu = suatChieu;
            ViewBag.Ghe = Ghe;
            ViewBag.MenuBapNuoc = menuBapNuoc;
            ViewBag.TongTienVe = donHang.TongTienBanDau;

            ViewBag.ExpireTime = donHang.NgayTao.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss");
            ViewBag.JsonBapNuoc = HttpContext.Session.GetString("DatVe_GioHangBapNuocTam") ?? "[]";

            return View("XacNhan");
        }

        [HttpPost]
        public async Task<IActionResult> HuyDonHangTam(string maDonHang)
        {
            var donHang = await _context.DonHang.FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);
            if (donHang != null && donHang.TrangThai == "ChoThanhToan")
            {
                donHang.TrangThai = "DaHuy";

                var ChiTietVe = await _context.ChiTietVe.Where(v => v.MaDonHang == maDonHang).ToListAsync();
                foreach (var ve in ChiTietVe) ve.TrangThai = "DaHuy";

                await _context.SaveChangesAsync();
            }
            HttpContext.Session.Remove("DatVe_MaDonHang_Tam");
            HttpContext.Session.Remove("DatVe_GioHangBapNuocTam");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SyncBapNuocTam(string jsonBapNuoc)
        {
            HttpContext.Session.SetString("DatVe_GioHangBapNuocTam", jsonBapNuoc ?? "[]");
            return Json(new { success = true });
        }
    }
}