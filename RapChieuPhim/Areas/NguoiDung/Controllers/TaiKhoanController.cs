using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.ViewModels;
using RapChieuPhim.Services;
using RapChieuPhim.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace RapChieuPhim.Areas.NguoiDung.Controllers
{
    [Area("NguoiDung")]
    public class TaiKhoanController : Controller
    {
        private readonly AccountService _accountService;
        private readonly AppDbContext _context;

        public TaiKhoanController(AccountService accountService, AppDbContext context)
        {
            _accountService = accountService;
            _context = context;
        }

        // GET: Hiển thị form Đăng nhập
        [HttpGet]
        public IActionResult DangNhap(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Xử lý submit form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(DangNhapViewModel model, string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (ModelState.IsValid)
            {
                var tk = await _accountService.DangNhapAsync(model.TenDangNhap, model.MatKhau);

                if (tk != null)
                {
                    _accountService.LuuSession(HttpContext, tk);

                    if (tk.VaiTro == "KhachHang")
                    {
                        var pendingOrderMa = HttpContext.Session.GetString("DatVe_MaDonHang_Tam");
                        if (!string.IsNullOrEmpty(pendingOrderMa))
                        {
                            var pendingOrder = await _context.DonHang.FirstOrDefaultAsync(d => d.MaDonHang == pendingOrderMa && d.TrangThai == "ChoThanhToan");

                            if (pendingOrder != null && pendingOrder.NgayTao.AddMinutes(5) > DateTime.Now)
                            {
                                pendingOrder.MaKhachHang = tk.MaKhachHang;
                                await _context.SaveChangesAsync();
                                TempData["PendingOrder"] = pendingOrderMa;
                            }
                            else
                            {
                                HttpContext.Session.Remove("DatVe_MaDonHang_Tam");
                            }
                        }

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                        return RedirectToAction("Index", "Home", new { area = "NguoiDung" });
                    }
                    else if (tk.VaiTro == "NhanVien")
                    {
                        return RedirectToAction("Index", "BanHang", new { area = "RapPhim" });
                    }
                    else if (tk.VaiTro == "Admin")
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "RapPhim" });
                    }
                }
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại, sai mật khẩu hoặc đã bị khóa.");
            }
            return View(model);
        }

        // =======================================================
        // LUỒNG ĐĂNG NHẬP GOOGLE
        // =======================================================

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded) return RedirectToAction("DangNhap");

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (email == null) return RedirectToAction("DangNhap");

            // Kiểm tra xem khách hàng này đã từng đăng nhập chưa
            var khachHang = await _context.KhachHang.FirstOrDefaultAsync(k => k.Email == email);
            TaiKhoan taiKhoan = null;

            if (khachHang == null)
            {
                // 1. CHƯA CÓ -> TẠO KHÁCH HÀNG MỚI
                khachHang = new KhachHang
                {
                    MaKhachHang = "KH" + DateTime.Now.Ticks.ToString().Substring(10),
                    HoTen = name ?? "Khách hàng Google",
                    Email = email,
                    GioiTinh = "Khác",
                    // Fix lỗi DateOnly: Dùng DateOnly.FromDateTime để cắt bỏ giờ/phút/giây
                    NgaySinh = DateOnly.FromDateTime(DateTime.Now.AddYears(-18)),
                    DaXoa = false
                };
                _context.KhachHang.Add(khachHang);

                // 2. TẠO TÀI KHOẢN MỚI CHO KHÁCH HÀNG NÀY
                taiKhoan = new TaiKhoan
                {
                    TenDangNhap = email,
                    MatKhau = "Google@123", // Mật khẩu ảo cho tài khoản mượn quyền Google
                    VaiTro = "KhachHang",
                    TrangThai = "DangHoatDong",
                    MaKhachHang = khachHang.MaKhachHang, // Liên kết (Foreign Key)
                    DaXoa = false
                };
                _context.TaiKhoan.Add(taiKhoan);

                await _context.SaveChangesAsync();
            }
            else
            {
                // Nếu khách đã tồn tại, tìm Tài khoản của họ để check xem có bị khóa không
                taiKhoan = await _context.TaiKhoan.FirstOrDefaultAsync(t => t.MaKhachHang == khachHang.MaKhachHang);
                if (taiKhoan != null && taiKhoan.TrangThai == "BiKhoa")
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa.");
                    return View("DangNhap");
                }
            }

            // ĐĂNG NHẬP THÀNH CÔNG -> Lưu Session y hệt như đăng nhập thường
            HttpContext.Session.SetString("MaKhachHang", khachHang.MaKhachHang);
            HttpContext.Session.SetString("TenDangNhap", taiKhoan?.TenDangNhap ?? email);
            HttpContext.Session.SetString("HoTen", khachHang.HoTen);
            HttpContext.Session.SetString("VaiTro", taiKhoan?.VaiTro ?? "KhachHang");

            // Đăng xuất khỏi Cookie tạm của Google
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        // =======================================================

        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        // POST: Xử lý form đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(DangKyViewModel model)
        {
            // K5: Kiểm tra mật khẩu khớp
            if (model.MatKhau != model.XacNhanMatKhau)
            {
                ModelState.AddModelError("XacNhanMatKhau", "Mat khau xac nhan khong khop.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            var (thanhCong, thongBao, tenDangNhap) = await _accountService.DangKyAsync(model);

            if (!thanhCong)
            {
                // K4.a: Email trùng
                if (thongBao.Contains("Email") || thongBao.Contains("email"))
                    ModelState.AddModelError("Email", thongBao);
                // Tên đăng nhập trùng
                else if (thongBao.Contains("Ten dang nhap") || thongBao.Contains("ten dang nhap"))
                    ModelState.AddModelError("TenDangNhap", thongBao);
                // K6.a: Mật khẩu yếu
                else
                    ModelState.AddModelError("MatKhau", thongBao);
                return View(model);
            }

            // K8: Gửi email xác minh
            string? token = VerifyTokenStore.GetTokenByUser(tenDangNhap!);
            if (token != null)
                await _accountService.GuiEmailXacMinhAsync(model.Email, tenDangNhap!, token);

            // K9: Chuyển đến trang chờ xác minh
            TempData["DangKyEmail"] = model.Email;
            TempData["DangKyTenDN"] = tenDangNhap;
            return RedirectToAction("ChoXacMinh");
        }

        // K9: Trang chờ xác minh email
        [HttpGet]
        public IActionResult ChoXacMinh()
        {
            ViewBag.Email = TempData["DangKyEmail"];
            ViewBag.TenDN = TempData["DangKyTenDN"];
            return View();
        }

        // K10: Người dùng click link trong email
        [HttpGet]
        public async Task<IActionResult> XacMinhEmail(string token, string user)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(user))
            {
                TempData["XacMinhError"] = "Link xac minh khong hop le.";
                return RedirectToAction("DangNhap");
            }

            var (thanhCong, thongBao) = await _accountService.XacMinhEmailAsync(token, user);

            if (thanhCong)
                TempData["XacMinhSuccess"] = thongBao;
            else
            {
                // K10.a: Link hết hạn
                TempData["XacMinhError"] = thongBao;
                TempData["XacMinhUser"] = user;
            }
            return RedirectToAction("DangNhap");
        }

        // K10.a: Gửi lại email xác minh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiLaiXacMinh(string email, string tenDangNhap)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(tenDangNhap))
                return RedirectToAction("DangKy");

            string token = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            VerifyTokenStore.Save(token, email, tenDangNhap);
            await _accountService.GuiEmailXacMinhAsync(email, tenDangNhap, token);

            TempData["DangKyEmail"] = email;
            TempData["DangKyTenDN"] = tenDangNhap;
            return RedirectToAction("ChoXacMinh");
        }

        // G1→G2: Bắt đầu đăng ký / đăng nhập bằng Google
        [HttpGet]
        public IActionResult GoogleLogin1()
        {
            var redirectUrl = Url.Action("GoogleCallback", "TaiKhoan", new { area = "NguoiDung" });
            var props = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(props,
                Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        }

        // G3→G7: Google trả về kết quả
        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(
                Microsoft.AspNetCore.Authentication.Cookies
                    .CookieAuthenticationDefaults.AuthenticationScheme);

            // G3.a: Người dùng từ chối cấp quyền
            if (!result.Succeeded || result.Principal == null)
            {
                TempData["GoogleError"] = "Ban da huy dang nhap bang Google.";
                return RedirectToAction("DangKy");
            }

            // G4: Lấy email và họ tên từ Google
            var claims = result.Principal.Claims.ToList();
            string? email = claims.FirstOrDefault(c =>
                c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            string? hoTen = claims.FirstOrDefault(c =>
                c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["GoogleError"] = "Khong the lay thong tin email tu Google.";
                return RedirectToAction("DangKy");
            }
            hoTen ??= email.Split('@')[0];

            // G5 → G6 → G7
            var (taiKhoan, laKhachMoi, thongBao) =
                await _accountService.XuLyGoogleCallbackAsync(email, hoTen);

            if (taiKhoan == null)
            {
                // G5.a: Tài khoản bị khóa hoặc chờ xác minh
                TempData["GoogleError"] = thongBao;
                return RedirectToAction("DangNhap");
            }

            // G7: Lưu session và chuyển về trang chủ
            _accountService.LuuSession(HttpContext, taiKhoan);
            TempData["GoogleSuccess"] = thongBao;
            return RedirectToAction("Index", "Home", new { area = "NguoiDung" });
        }

        [HttpGet]
        public IActionResult DangXuat()
        {
            _accountService.DangXuat(HttpContext);
            return RedirectToAction("Index", "Home");
        }

        // ... Các hàm ThongTin, QuenMatKhau, XacNhanOTP giữ nguyên không đổi ...
        [HttpGet]
        public async Task<IActionResult> ThongTin([FromServices] RapChieuPhim.Data.AppDbContext context)
        {
            var maKh = HttpContext.Session.GetString("MaKhachHang");
            if (string.IsNullOrEmpty(maKh)) return RedirectToAction("DangNhap");

            var kh = await context.KhachHang.FindAsync(maKh);
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

        [HttpGet]
        public IActionResult QuenMatKhau() => View();

        [HttpPost]
        public async Task<IActionResult> QuenMatKhau(QuenMatKhauViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

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
                    HttpContext.Session.Remove("BlockUntil");
                    HttpContext.Session.Remove("ResendCount");
                }
            }

            bool isExist = await _accountService.KiemTraEmailTonTaiAsync(model.Email);
            if (!isExist)
            {
                ModelState.AddModelError("", "Email này chưa được đăng ký trong hệ thống.");
                return View(model);
            }

            int count = HttpContext.Session.GetInt32("ResendCount") ?? 0;
            count++;
            HttpContext.Session.SetInt32("ResendCount", count);

            if (count > 5)
            {
                var lockTime = DateTime.Now.AddHours(1);
                HttpContext.Session.SetString("BlockUntil", lockTime.ToString());
                ModelState.AddModelError("", "Hệ thống phát hiện spam. Tính năng này đã bị tạm khóa 1 tiếng.");
                return View(model);
            }

            string otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("ResetOTP", otp);
            HttpContext.Session.SetString("ResetEmail", model.Email);

            bool isSent = await _accountService.GuiEmailOTPAsync(model.Email, otp);
            if (!isSent)
            {
                ModelState.AddModelError("", "Lỗi gửi Email. Nghĩa hãy kiểm tra lại Mật khẩu ứng dụng Google nhé.");
                return View(model);
            }

            return RedirectToAction("XacNhanOTP");
        }

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
                    HttpContext.Session.Remove("ResetOTP");
                    HttpContext.Session.Remove("ResetEmail");
                    HttpContext.Session.Remove("CaptchaCode");

                    return RedirectToAction("DangNhap");
                }
            }
            return View(model);
        }

        [Route("api/captcha")]
        public IActionResult GetCaptchaImage()
        {
            string captcha = new Random().Next(1000, 9999).ToString();
            HttpContext.Session.SetString("CaptchaCode", captcha);

            string svg = $@"<svg width='120' height='40' xmlns='http://www.w3.org/2000/svg'>
                             <rect width='100%' height='100%' fill='#f0f0f0'/>
                             <text x='50%' y='50%' font-size='24' font-weight='bold' font-family='monospace' fill='#034ea2' dominant-baseline='middle' text-anchor='middle' transform='rotate({new Random().Next(-5, 5)}, 60, 20)'>{captcha}</text>
                             <line x1='0' y1='{new Random().Next(10, 30)}' x2='120' y2='{new Random().Next(10, 30)}' stroke='red' stroke-width='2'/>
                           </svg>";

            return Content(svg, "image/svg+xml");
        }
    }
}