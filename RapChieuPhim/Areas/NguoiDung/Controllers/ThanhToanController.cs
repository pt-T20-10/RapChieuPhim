using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using RapChieuPhim.Data;
using RapChieuPhim.Extensions;
using RapChieuPhim.Models.Entities;
using RapChieuPhim.Models.ViewModels;
using RapChieuPhim.Services;

namespace RapChieuPhim.Areas.NguoiDung.Controllers
{
    [Area("NguoiDung")]
    public class ThanhToanController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PayOSClient _payOS;
        private readonly AccountService _accountService;

        public ThanhToanController(AppDbContext context, PayOSClient payOS, AccountService accountService)
        {
            _context = context;
            _payOS = payOS;
            _accountService = accountService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var gioHang = HttpContext.Session.Get<List<CartItem>>("GioHangBapNuoc") ?? new List<CartItem>();
            string loaiGiaoDich = HttpContext.Session.GetString("LoaiGiaoDich");
            string maDonHangTam = HttpContext.Session.GetString("DatVe_MaDonHang_Tam");

            if (loaiGiaoDich != "DatVe" && !gioHang.Any()) return RedirectToAction("Index", "DichVu");

            ViewBag.GioHang = gioHang;
            string maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            ViewBag.IsLogined = !string.IsNullOrEmpty(maKhachHang);

            if (!string.IsNullOrEmpty(maKhachHang))
            {
                var khachHang = await _context.KhachHang.FindAsync(maKhachHang);
                if (khachHang != null)
                {
                    ViewBag.HoTen = khachHang.HoTen;
                    ViewBag.Email = khachHang.Email;
                    ViewBag.SoDienThoai = khachHang.SoDienThoai ?? "";
                }
            }

            double tongTienVe = 0;
            if (loaiGiaoDich == "DatVe" && !string.IsNullOrEmpty(maDonHangTam))
            {
                var dhTam = await _context.DonHang.FindAsync(maDonHangTam);
                if (dhTam != null) tongTienVe = dhTam.TongTienBanDau;
            }

            double tongTienBapNuoc = gioHang.Sum(x => x.ThanhTien);
            double tongTien = tongTienVe + tongTienBapNuoc;

            string maKhuyenMai = HttpContext.Session.GetString("MaKhuyenMai");
            double tienGiam = 0;
            string strTienGiam = HttpContext.Session.GetString("SoTienGiam");
            if (!string.IsNullOrEmpty(maKhuyenMai) && !string.IsNullOrEmpty(strTienGiam))
            {
                tienGiam = double.Parse(strTienGiam);
            }

            ViewBag.TongTienVe = tongTienVe;
            ViewBag.TongTien = tongTien;
            ViewBag.TienGiam = tienGiam;
            ViewBag.TongTienSauGiam = tongTien - tienGiam;
            ViewBag.MaKhuyenMai = maKhuyenMai;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TaoThanhToan(string hoTen, string email, string soDienThoai)
        {
            var gioHang = HttpContext.Session.Get<List<CartItem>>("GioHangBapNuoc") ?? new List<CartItem>();
            string loaiGiaoDich = HttpContext.Session.GetString("LoaiGiaoDich");
            string maDonHangTam = HttpContext.Session.GetString("DatVe_MaDonHang_Tam");
            string maKhachHang = HttpContext.Session.GetString("MaKhachHang");

            if (loaiGiaoDich != "DatVe" && !gioHang.Any()) return BadRequest("Giỏ hàng trống.");

            long orderCode;
            DonHang donHang;

            if (loaiGiaoDich == "DatVe" && !string.IsNullOrEmpty(maDonHangTam))
            {
                donHang = await _context.DonHang.Include(d => d.ChiTietVe).FirstOrDefaultAsync(d => d.MaDonHang == maDonHangTam);
                if (donHang == null || donHang.TrangThai == "DaHuy") return BadRequest("Đơn hàng không hợp lệ hoặc đã lố 5 phút.");

                orderCode = long.Parse(donHang.MaDonHang);
                donHang.MaKhachHang = string.IsNullOrEmpty(maKhachHang) ? null : maKhachHang;
            }
            else
            {
                orderCode = long.Parse(DateTime.Now.ToString("yyMMddHHmmss"));
                donHang = new DonHang
                {
                    MaDonHang = orderCode.ToString(),
                    MaKhachHang = string.IsNullOrEmpty(maKhachHang) ? null : maKhachHang,
                    NgayTao = DateTime.Now,
                    TrangThai = "ChoThanhToan",
                    DaXoa = false
                };
                _context.DonHang.Add(donHang);
            }

            foreach (var item in gioHang)
            {
                var ct = new ChiTietDichVu
                {
                    MaChiTiet = "CT" + DateTime.Now.Ticks.ToString().Substring(10),
                    MaDonHang = donHang.MaDonHang,
                    MaDichVu = item.MaDichVu,
                    SoLuong = item.SoLuong,
                    DonGia = item.GiaBan,
                    DaXoa = false
                };
                _context.ChiTietDichVu.Add(ct);
            }

            double tongTienVe = donHang.ChiTietVe?.Sum(v => v.GiaVe) ?? 0;
            double tongTienBapNuoc = gioHang.Sum(x => x.ThanhTien);
            double tongTienBanDau = tongTienVe + tongTienBapNuoc;

            string maKhuyenMai = HttpContext.Session.GetString("MaKhuyenMai");
            string strTongTienSauGiam = HttpContext.Session.GetString("TongTienSauGiam");
            double tongTienSauGiam = string.IsNullOrEmpty(strTongTienSauGiam) ? tongTienBanDau : double.Parse(strTongTienSauGiam);

            donHang.MaKhuyenMai = string.IsNullOrEmpty(maKhuyenMai) ? null : maKhuyenMai;
            donHang.TongTienBanDau = tongTienBanDau;
            donHang.TongTienSauGiam = tongTienSauGiam;

            if (tongTienBanDau > 0 && tongTienSauGiam < tongTienBanDau)
            {
                double tyLeTra = tongTienSauGiam / tongTienBanDau;

                if (donHang.ChiTietVe != null)
                {
                    foreach (var ve in donHang.ChiTietVe)
                    {
                        ve.GiaVe = ve.GiaVe * tyLeTra;
                    }
                }

                if (donHang.ChiTietDichVu != null)
                {
                    foreach (var dv in donHang.ChiTietDichVu)
                    {
                        dv.DonGia = dv.DonGia * tyLeTra;
                    }
                }
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("GuestEmail", email);

            var domain = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            string payOsDescription = $"TT {(loaiGiaoDich == "DatVe" ? "Ve" : "DichVu")} {DateTime.Now:ddMMyyHHmm}";

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)tongTienSauGiam,
                Description = payOsDescription,
                CancelUrl = $"{domain}/NguoiDung/ThanhToan/KetQua?orderCode={orderCode}&cancel=true",
                ReturnUrl = $"{domain}/NguoiDung/ThanhToan/KetQua?orderCode={orderCode}&cancel=false"
            };

            var paymentLink = await _payOS.PaymentRequests.CreateAsync(paymentRequest);
            return Redirect(paymentLink.CheckoutUrl);
        }

        [HttpGet]
        [Route("NguoiDung/ThanhToan/KetQua")]
        public async Task<IActionResult> KetQua()
        {
            string orderCodeStr = HttpContext.Request.Query["orderCode"];
            string cancelStr = HttpContext.Request.Query["cancel"];
            string status = HttpContext.Request.Query["status"];

            if (string.IsNullOrEmpty(orderCodeStr)) return RedirectToAction("Index", "Home");

            long orderCode = long.Parse(orderCodeStr);
            bool isCancelled = cancelStr?.ToLower() == "true" || status == "CANCELLED";

            var donHang = await _context.DonHang
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaKhuyenMaiNavigation)
                .Include(d => d.ChiTietVe).ThenInclude(v => v.MaSuatChieuNavigation).ThenInclude(s => s.MaPhimNavigation)
                .Include(d => d.ChiTietVe).ThenInclude(v => v.MaGheNavigation)
                .Include(d => d.ChiTietDichVu).ThenInclude(dv => dv.MaDichVuNavigation)
                .FirstOrDefaultAsync(d => d.MaDonHang == orderCode.ToString());

            if (donHang == null) return Content("Không tìm thấy đơn hàng.");

            // LẤY LOẠI GIAO DỊCH TRƯỚC KHI XÓA
            string loaiGiaoDich = HttpContext.Session.GetString("LoaiGiaoDich");

            if (isCancelled)
            {
                if (donHang.TrangThai == "ChoThanhToan")
                {
                    donHang.TrangThai = "DaHuy";
                    if (donHang.ChiTietVe != null)
                    {
                        foreach (var ve in donHang.ChiTietVe) ve.TrangThai = "DaHuy";
                    }
                    await _context.SaveChangesAsync();
                }

                DonDepSession();
                return Content("<script>alert('Bạn đã hủy thanh toán.'); window.location.href='/';</script>", "text/html; charset=utf-8");
            }

            if (status == "PAID" && donHang.TrangThai == "ChoThanhToan")
            {
                donHang.TrangThai = "DaThanhToan";
                if (donHang.ChiTietVe != null)
                {
                    foreach (var ve in donHang.ChiTietVe) ve.TrangThai = "ChuaSuDung";
                }
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(donHang.MaKhachHang))
                {
                    var khach = await _context.KhachHang.FindAsync(donHang.MaKhachHang);
                    if (khach != null && !string.IsNullOrEmpty(khach.Email))
                    {
                        _ = _accountService.GuiEmailHoaDonAsync(donHang, khach.Email);
                    }
                }
            }

            // GỌI HÀM DỌN DẸP
            DonDepSession();

            // ĐIỀU HƯỚNG TẠI ĐÂY (Chỗ này mới biết donHang là ai)
            if (loaiGiaoDich == "BanVeTrucTiep")
            {
                return RedirectToAction("InVe", "BanHang", new { area = "RapPhim", maDonHang = donHang.MaDonHang });
            }

            if (loaiGiaoDich == "BanDichVuTrucTiep")
            {
                return RedirectToAction("InHoaDon", "BanHang", new { area = "RapPhim", maDonHang = donHang.MaDonHang });
            }

            return View(donHang);
        }

        // HÀM NÀY CHỈ ĐỂ XÓA RÁC, KHÔNG ĐƯỢC CHỨA RETURN!
        private void DonDepSession()
        {
            HttpContext.Session.Remove("GioHangBapNuoc");
            HttpContext.Session.Remove("DatVe_MaDonHang_Tam");
            HttpContext.Session.Remove("DatVe_GioHangBapNuocTam");
            HttpContext.Session.Remove("MaKhuyenMai");
            HttpContext.Session.Remove("SoTienGiam");
            HttpContext.Session.Remove("TongTienSauGiam");
            HttpContext.Session.Remove("LoaiGiaoDich");
        }
    }
}