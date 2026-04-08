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

        // 1. HIỂN THỊ TRANG CHECKOUT (Hợp nhất Đặt Vé & Bắp Nước)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var gioHang = HttpContext.Session.Get<List<CartItem>>("GioHangBapNuoc") ?? new List<CartItem>();
            string loaiGiaoDich = HttpContext.Session.GetString("LoaiGiaoDich");
            string maDonHangTam = HttpContext.Session.GetString("DatVe_MaDonHang_Tam");

            // Chỉ chặn không cho vào Checkout nếu KHÔNG PHẢI đặt vé VÀ giỏ bắp nước cũng trống
            if (loaiGiaoDich != "DatVe" && !gioHang.Any()) return RedirectToAction("Index", "DichVu");

            ViewBag.GioHang = gioHang;

            // Lấy thông tin user
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

            // Gộp tiền Vé (nếu có) và tiền Bắp nước
            double tongTienVe = 0;
            if (loaiGiaoDich == "DatVe" && !string.IsNullOrEmpty(maDonHangTam))
            {
                var dhTam = await _context.DonHang.FindAsync(maDonHangTam);
                if (dhTam != null) tongTienVe = dhTam.TongTienBanDau;
            }

            double tongTienBapNuoc = gioHang.Sum(x => x.ThanhTien);
            double tongTien = tongTienVe + tongTienBapNuoc; // Tổng tiền thực tế

            // Lấy thông tin Giảm giá
            string maKhuyenMai = HttpContext.Session.GetString("MaKhuyenMai");
            double tienGiam = 0;
            string strTienGiam = HttpContext.Session.GetString("SoTienGiam");
            if (!string.IsNullOrEmpty(maKhuyenMai) && !string.IsNullOrEmpty(strTienGiam))
            {
                tienGiam = double.Parse(strTienGiam);
            }

            ViewBag.TongTienVe = tongTienVe; // Truyền ra để hiển thị riêng dòng Vé
            ViewBag.TongTien = tongTien;
            ViewBag.TienGiam = tienGiam;
            ViewBag.TongTienSauGiam = tongTien - tienGiam;
            ViewBag.MaKhuyenMai = maKhuyenMai;

            return View();
        }

        // 2. XỬ LÝ GỬI THÔNG TIN VÀ TẠO LINK PAYOS
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

            // NẾU LÀ ĐẶT VÉ -> Tái sử dụng Đơn Hàng Tạm đã có
            if (loaiGiaoDich == "DatVe" && !string.IsNullOrEmpty(maDonHangTam))
            {
                donHang = await _context.DonHang.Include(d => d.ChiTietVe).FirstOrDefaultAsync(d => d.MaDonHang == maDonHangTam);
                if (donHang == null || donHang.TrangThai == "DaHuy") return BadRequest("Đơn hàng không hợp lệ hoặc đã lố 5 phút.");

                orderCode = long.Parse(donHang.MaDonHang);
                donHang.MaKhachHang = string.IsNullOrEmpty(maKhachHang) ? null : maKhachHang; // Gắn tên user nếu họ vừa login
            }
            else // NẾU CHỈ MUA BẮP NƯỚC -> Tạo đơn hàng mới
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

            // Lưu Chi Tiết Bắp Nước vào đơn hàng
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

            // Cập nhật lại tổng tiền (Vé + Bắp Nước)
            double tongTienVe = donHang.ChiTietVe?.Sum(v => v.GiaVe) ?? 0;
            double tongTienBapNuoc = gioHang.Sum(x => x.ThanhTien);
            double tongTienBanDau = tongTienVe + tongTienBapNuoc;

            string maKhuyenMai = HttpContext.Session.GetString("MaKhuyenMai");
            string strTongTienSauGiam = HttpContext.Session.GetString("TongTienSauGiam");
            double tongTienSauGiam = string.IsNullOrEmpty(strTongTienSauGiam) ? tongTienBanDau : double.Parse(strTongTienSauGiam);

            donHang.MaKhuyenMai = string.IsNullOrEmpty(maKhuyenMai) ? null : maKhuyenMai;
            donHang.TongTienBanDau = tongTienBanDau;
            donHang.TongTienSauGiam = tongTienSauGiam;

            // ==========================================
            // THỦ THUẬT ÉP GIÁ (DISCOUNT ALLOCATION)
            // ==========================================
            if (tongTienBanDau > 0 && tongTienSauGiam < tongTienBanDau)
            {
                // Tính tỷ lệ giá phải trả (Ví dụ: Giảm 20k trên tổng 100k -> Tỷ lệ trả là 0.8)
                double tyLeTra = tongTienSauGiam / tongTienBanDau;

                // 1. Ép giá vé
                if (donHang.ChiTietVe != null)
                {
                    foreach (var ve in donHang.ChiTietVe)
                    {
                        ve.GiaVe = ve.GiaVe * tyLeTra; // Cập nhật lại giá vé thực thu
                    }
                }

                // 2. Ép đơn giá bắp nước (Cột ThanhTien trong DB sẽ tự tính theo DonGia mới này)
                if (donHang.ChiTietDichVu != null)
                {
                    foreach (var dv in donHang.ChiTietDichVu)
                    {
                        dv.DonGia = dv.DonGia * tyLeTra; // Cập nhật lại đơn giá bắp nước thực thu
                    }
                }
            }
            // ==========================================

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

        // 3. HÀM CHỜ CALLBACK TỪ PAYOS (PHIÊN BẢN CHỐNG 404 TUYỆT ĐỐI)
        [HttpGet]
        [Route("NguoiDung/ThanhToan/KetQua")]
        public async Task<IActionResult> KetQua()
        {
            // 1. Lấy dữ liệu từ URL
            string orderCodeStr = HttpContext.Request.Query["orderCode"];
            string cancelStr = HttpContext.Request.Query["cancel"];
            string status = HttpContext.Request.Query["status"];

            if (string.IsNullOrEmpty(orderCodeStr)) return RedirectToAction("Index", "Home");

            long orderCode = long.Parse(orderCodeStr);
            bool isCancelled = cancelStr?.ToLower() == "true" || status == "CANCELLED";

            // 2. Tìm đơn hàng (Load đủ Data để in Hóa đơn)
            var donHang = await _context.DonHang
                .Include(d => d.MaKhachHangNavigation)
                .Include(d => d.MaKhuyenMaiNavigation)
                .Include(d => d.ChiTietVe).ThenInclude(v => v.MaSuatChieuNavigation).ThenInclude(s => s.MaPhimNavigation)
                .Include(d => d.ChiTietVe).ThenInclude(v => v.MaGheNavigation)
                .Include(d => d.ChiTietDichVu).ThenInclude(dv => dv.MaDichVuNavigation)
                .FirstOrDefaultAsync(d => d.MaDonHang == orderCode.ToString());

            if (donHang == null) return Content("Không tìm thấy đơn hàng.");

            // 3. XỬ LÝ KHI HỦY
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
                // Xóa session
                DonDepSession();
                return Content("<script>alert('Bạn đã hủy thanh toán.'); window.location.href='/';</script>", "text/html; charset=utf-8");
            }

            // 4. XỬ LÝ KHI THÀNH CÔNG
            if (status == "PAID" && donHang.TrangThai == "ChoThanhToan")
            {
                donHang.TrangThai = "DaThanhToan";
                if (donHang.ChiTietVe != null)
                {
                    foreach (var ve in donHang.ChiTietVe) ve.TrangThai = "ChuaSuDung";
                }
                await _context.SaveChangesAsync();

                // Gửi mail (Nhớ thay mật khẩu ứng dụng của Nghĩa vào AccountService)
                if (!string.IsNullOrEmpty(donHang.MaKhachHang))
                {
                    var khach = await _context.KhachHang.FindAsync(donHang.MaKhachHang);
                    if (khach != null && !string.IsNullOrEmpty(khach.Email))
                    {
                        _ = _accountService.GuiEmailHoaDonAsync(donHang, khach.Email);
                    }
                }
            }

            DonDepSession();
            return View(donHang);
        }

        // Hàm phụ để code sạch hơn
        private void DonDepSession()
        {
            HttpContext.Session.Remove("GioHangBapNuoc");
            HttpContext.Session.Remove("DatVe_MaDonHang_Tam");
            HttpContext.Session.Remove("DatVe_GioHangBapNuocTam");
            HttpContext.Session.Remove("MaKhuyenMai");
            HttpContext.Session.Remove("SoTienGiam");
            HttpContext.Session.Remove("TongTienSauGiam");

            string loaiGiaoDich = HttpContext.Session.GetString("LoaiGiaoDich");
            HttpContext.Session.Remove("LoaiGiaoDich");

            if (loaiGiaoDich == "BanVeTrucTiep")
            {
                return RedirectToAction("InVe", "BanHang",
                    new { area = "RapPhim", maDonHang = donHang.MaDonHang });
            }

            if (loaiGiaoDich == "BanDichVuTrucTiep")
            {
                return RedirectToAction("InHoaDon", "BanHang",
                    new { area = "RapPhim", maDonHang = donHang.MaDonHang });
            }

            return View(donHang);
        }


    }
}