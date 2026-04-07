using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Models.ViewModels;
using RapChieuPhim.Services;

namespace RapChieuPhim.Areas.NguoiDung.Controllers
{
    [Area("NguoiDung")]
    public class TaiKhoanController : Controller
    {
        private readonly AccountService _accountService;

        public TaiKhoanController(AccountService accountService)
        {
            _accountService = accountService;
        }

        // GET: Hiển thị form Đăng nhập
        [HttpGet]
        public IActionResult DangNhap()
        {
            // Nếu đã đăng nhập rồi thì đá về trang chủ, không cho vào trang đăng nhập nữa
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("TenDangNhap")))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Xử lý submit form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(DangNhapViewModel model)
        {
            if (ModelState.IsValid)
            {
                var tk = await _accountService.DangNhapAsync(model.TenDangNhap, model.MatKhau);

                if (tk != null)
                {
                    // Đăng nhập thành công -> Lưu Session
                    _accountService.LuuSession(HttpContext, tk);

                    // Điều hướng dựa theo Role (VaiTro) chính xác theo thiết kế hệ thống
                    if (tk.VaiTro == "KhachHang")
                    {
                        // Khách hàng -> Trang chủ
                        return RedirectToAction("Index", "Home", new { area = "NguoiDung" });
                    }
                    else if (tk.VaiTro == "NhanVien")
                    {
                        // Nhân viên -> Trang Bán Vé & Dịch vụ (Controller BanHang)
                        return RedirectToAction("Index", "BanHang", new { area = "RapPhim" });
                    }
                    else if (tk.VaiTro == "Admin")
                    {
                        // Quản lý/Admin -> Trang Dashboard Tổng quan
                        return RedirectToAction("Index", "Dashboard", new { area = "RapPhim" });
                    }
                }

                // Trả về lỗi nếu sai tài khoản/mật khẩu
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại, sai mật khẩu hoặc đã bị khóa.");
            }
            return View(model);
        }

        // Action Đăng ký
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult DangXuat()
        {
            // Gọi hàm xóa Session từ Service
            _accountService.DangXuat(HttpContext);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ThongTin([FromServices] RapChieuPhim.Data.AppDbContext context)
        {
            var maKh = HttpContext.Session.GetString("MaKhachHang");
            if (string.IsNullOrEmpty(maKh)) return RedirectToAction("DangNhap");

            // Lấy thông tin cũ từ DB đưa lên Form
            var kh = await context.KhachHangs.FindAsync(maKh);
            if (kh == null) return NotFound();

            var model = new ThongTinViewModel
            {
                HoTen = kh.HoTen,
                Email = kh.Email,
                SoDienThoai = kh.SoDienThoai,
                NgaySinh = kh.NgaySinh,
                GioiTinh = kh.GioiTinh
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThongTin(ThongTinViewModel model)
        {
            var maKh = HttpContext.Session.GetString("MaKhachHang");
            if (string.IsNullOrEmpty(maKh)) return RedirectToAction("DangNhap");

            if (ModelState.IsValid)
            {
                var result = await _accountService.CapNhatThongTinAsync(maKh, model);
                if (result.ThanhCong)
                {
                    // Cập nhật lại Session tên hiển thị trên Header nếu họ đổi Tên
                    HttpContext.Session.SetString("HoTen", model.HoTen);
                    TempData["SuccessMessage"] = result.ThongBao;
                }
                else
                {
                    TempData["ErrorMessage"] = result.ThongBao;
                }
            }
            return View(model);
        }

        // --- 1. NHẬP EMAIL ---
        [HttpGet]
        public IActionResult QuenMatKhau() => View();

        [HttpPost]
        public async Task<IActionResult> QuenMatKhau(QuenMatKhauViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Kiểm tra xem có đang bị khóa (Block) 1 tiếng hay không
            var blockUntilStr = HttpContext.Session.GetString("BlockUntil");
            if (!string.IsNullOrEmpty(blockUntilStr))
            {
                var blockUntil = DateTime.Parse(blockUntilStr);
                if (DateTime.Now < blockUntil)
                {
                    var remaining = blockUntil - DateTime.Now;
                    ModelState.AddModelError("", $"Bạn đã gửi quá 5 lần. Vui lòng thử lại sau {remaining.Minutes} phút {remaining.Seconds} giây.");
                    return View(model);
                }
                else
                {
                    // Đã hết thời gian khóa -> Reset lại bộ đếm
                    HttpContext.Session.Remove("BlockUntil");
                    HttpContext.Session.Remove("ResendCount");
                }
            }

            // 2. Kiểm tra Email tồn tại
            bool isExist = await _accountService.KiemTraEmailTonTaiAsync(model.Email);
            if (!isExist)
            {
                ModelState.AddModelError("", "Email này chưa được đăng ký trong hệ thống.");
                return View(model);
            }

            // 3. Xử lý bộ đếm gửi lại (ResendCount)
            int count = HttpContext.Session.GetInt32("ResendCount") ?? 0;
            count++;
            HttpContext.Session.SetInt32("ResendCount", count);

            if (count > 5)
            {
                // Khóa tính năng trong 1 tiếng
                var lockTime = DateTime.Now.AddHours(1);
                HttpContext.Session.SetString("BlockUntil", lockTime.ToString());
                ModelState.AddModelError("", "Hệ thống phát hiện spam. Tính năng này đã bị tạm khóa 1 tiếng.");
                return View(model);
            }

            // 4. Sinh OTP mới (Ghi đè hoàn toàn mã cũ)
            string otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("ResetOTP", otp);
            HttpContext.Session.SetString("ResetEmail", model.Email);

            // 5. Gửi Mail
            bool isSent = await _accountService.GuiEmailOTPAsync(model.Email, otp);
            if (!isSent)
            {
                ModelState.AddModelError("", "Lỗi gửi Email. Nghĩa hãy kiểm tra lại Mật khẩu ứng dụng Google nhé.");
                return View(model);
            }

            return RedirectToAction("XacNhanOTP");
        }

        // --- 2. XÁC NHẬN OTP ---
        [HttpGet]
        public IActionResult XacNhanOTP() => View();

        [HttpPost]
        public IActionResult XacNhanOTP(string otpCode)
        {
            var sessionOtp = HttpContext.Session.GetString("ResetOTP");
            if (sessionOtp != null && sessionOtp == otpCode)
            {
                return RedirectToAction("DatLaiMatKhau");
            }

            ViewBag.Error = "Mã OTP không chính xác hoặc đã hết hạn.";
            return View();
        }

        // --- 3. ĐẶT LẠI MẬT KHẨU & CAPTCHA ---
        [HttpGet]
        public IActionResult DatLaiMatKhau() => View();

        [HttpPost]
        public async Task<IActionResult> DatLaiMatKhau(DatLaiMatKhauViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");
            if (sessionCaptcha == null || sessionCaptcha != model.CaptchaCode)
            {
                ModelState.AddModelError("CaptchaCode", "Mã Captcha không chính xác.");
                return View(model);
            }

            var email = HttpContext.Session.GetString("ResetEmail");
            if (email != null)
            {
                bool rs = await _accountService.DatLaiMatKhauAsync(email, model.MatKhauMoi);
                if (rs)
                {
                    // Đổi xong thì xóa Session rác
                    HttpContext.Session.Remove("ResetOTP");
                    HttpContext.Session.Remove("ResetEmail");
                    HttpContext.Session.Remove("CaptchaCode");

                    return RedirectToAction("DangNhap"); // Về lại form đăng nhập
                }
            }

            return View(model);
        }

        // --- API SINH ẢNH CAPTCHA CHẤT LƯỢNG ---
        [Route("api/captcha")]
        public IActionResult GetCaptchaImage()
        {
            string captcha = new Random().Next(1000, 9999).ToString();
            HttpContext.Session.SetString("CaptchaCode", captcha);

            // Dùng thư viện SkiaSharp hoặc trả về ảnh base64/SVG đơn giản. 
            // Để gọn nhất cho MVC, ta render 1 ảnh HTML dạng SVG
            string svg = $@"<svg width='120' height='40' xmlns='http://www.w3.org/2000/svg'>
                             <rect width='100%' height='100%' fill='#f0f0f0'/>
                             <text x='50%' y='50%' font-size='24' font-weight='bold' font-family='monospace' fill='#034ea2' dominant-baseline='middle' text-anchor='middle' transform='rotate({new Random().Next(-5, 5)}, 60, 20)'>{captcha}</text>
                             <line x1='0' y1='{new Random().Next(10, 30)}' x2='120' y2='{new Random().Next(10, 30)}' stroke='red' stroke-width='2'/>
                           </svg>";

            return Content(svg, "image/svg+xml");
        }
    }
}