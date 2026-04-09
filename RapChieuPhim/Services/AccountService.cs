using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;
using RapChieuPhim.Models.ViewModels;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace RapChieuPhim.Services
{
    public class AccountService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AccountService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // 1. Hàm xử lý đăng nhập
        public async Task<TaiKhoan?> DangNhapAsync(string tenDangNhap, string matKhau)
        {
            var taiKhoan = await _context.TaiKhoan
                .Include(t => t.MaKhachHangNavigation)
                .FirstOrDefaultAsync(t =>
                    (t.TenDangNhap == tenDangNhap || (t.MaKhachHangNavigation != null && t.MaKhachHangNavigation.Email == tenDangNhap))
                    && t.TrangThai == "HoatDong"
                    && t.DaXoa == false);

            if (taiKhoan == null)
                return null; // Không tìm thấy hoặc bị khóa

            // --- ĐOẠN CODE TEST: Tự động chuẩn hóa lại Hash mật khẩu ---
            if (taiKhoan.TenDangNhap == "nghia_admin")
            {
                // Tự động tạo hash chuẩn từ thư viện BCrypt.Net-Next 4.1.0 và ghi đè vào CSDL
                taiKhoan.MatKhau = BCrypt.Net.BCrypt.HashPassword("123456");
                _context.TaiKhoan.Update(taiKhoan);
                await _context.SaveChangesAsync();
            }
            // ------------------------------------------------------------

            // Dùng BCrypt để verify mật khẩu người dùng nhập với Hash trong DB
            bool isValid = BCrypt.Net.BCrypt.Verify(matKhau, taiKhoan.MatKhau);

            return isValid ? taiKhoan : null;
        }

        // 2. Hàm lưu Session theo chuẩn của Leader
        public void LuuSession(HttpContext context, TaiKhoan tk)
        {
            context.Session.SetString("MaKhachHang", tk.MaKhachHang ?? "");
            context.Session.SetString("HoTen", tk.MaKhachHangNavigation?.HoTen ?? "");
            context.Session.SetString("VaiTro", tk.VaiTro);
            context.Session.SetString("TenDangNhap", tk.TenDangNhap);
        }

        public async Task<(bool ThanhCong, string ThongBao)> CapNhatThongTinAsync(string maKhachHang, ThongTinViewModel model)
        {
            var khachHang = await _context.KhachHang.FindAsync(maKhachHang);
            if (khachHang == null) return (false, "Không tìm thấy khách hàng.");

            // Kiểm tra trùng Email nếu user đổi sang Email khác
            if (khachHang.Email != model.Email)
            {
                bool emailTonTai = await _context.KhachHang.AnyAsync(k => k.Email == model.Email);
                if (emailTonTai) return (false, "Email này đã được sử dụng bởi tài khoản khác.");
            }

            // Chỉ cập nhật những trường được thay đổi
            khachHang.HoTen = model.HoTen;
            khachHang.Email = model.Email;
            if (!string.IsNullOrEmpty(model.SoDienThoai)) khachHang.SoDienThoai = model.SoDienThoai;
            khachHang.NgaySinh = model.NgaySinh.Value;
            if (!string.IsNullOrEmpty(model.GioiTinh)) khachHang.GioiTinh = model.GioiTinh;

            _context.KhachHang.Update(khachHang);
            await _context.SaveChangesAsync();

            return (true, "Cập nhật thông tin thành công!");
        }

        // Gửi OTP
        public async Task<bool> GuiEmailOTPAsync(string toEmail, string otp)
        {
            try
            {
                var fromAddress = new MailAddress("nhoangnghia2104@gmail.com", "Rạp Chiếu Phim Nhóm 1");
                var toAddress = new MailAddress(toEmail);
                const string fromPassword = "xxx"; 

                string subject = "Mã xác nhận khôi phục mật khẩu";
                string body = $"Mã OTP của bạn là: <strong>{otp}</strong>. Mã này có hiệu lực trong 5 phút. Vui lòng không chia sẻ cho bất kỳ ai.";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    await smtp.SendMailAsync(message);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Đổi mật khẩu
        public async Task<bool> DatLaiMatKhauAsync(string email, string matKhauMoi)
        {
            // Tìm Khách hàng qua Email
            var khachHang = await _context.KhachHang.FirstOrDefaultAsync(k => k.Email == email);
            if (khachHang == null) return false;

            // Tìm Tài khoản liên kết với Khách hàng đó
            var taiKhoan = await _context.TaiKhoan.FirstOrDefaultAsync(t => t.MaKhachHang == khachHang.MaKhachHang);
            if (taiKhoan == null) return false;

            // Hash mật khẩu mới bằng BCrypt
            taiKhoan.MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhauMoi);
            _context.TaiKhoan.Update(taiKhoan);
            await _context.SaveChangesAsync();
            return true;
        }

        // Hàm kiểm tra Email có tồn tại trong hệ thống không
        public async Task<bool> KiemTraEmailTonTaiAsync(string email)
        {
            return await _context.KhachHang.AnyAsync(k => k.Email == email);
        }

        // Hàm Đăng xuất (Làm luôn cho tiện)
        public void DangXuat(HttpContext context)
        {
            context.Session.Clear(); // Xóa sạch session hiện tại
        }

        public async Task<bool> GuiEmailHoaDonAsync(DonHang donHang, string toEmail)
        {
            try
            {
                var fromAddress = new MailAddress("nhoangnghia2104@gmail.com", "B E T A Cinemas");
                var toAddress = new MailAddress(toEmail);
                const string fromPassword = "xxx";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                // 1. Dựng HTML phần Vé Phim (Đã bỏ QR)
                StringBuilder htmlVe = new StringBuilder();
                if (donHang.ChiTietVe != null && donHang.ChiTietVe.Any())
                {
                    var suat = donHang.ChiTietVe.First().MaSuatChieuNavigation;
                    var danhSachGhe = string.Join(", ", donHang.ChiTietVe.Select(v => v.MaGheNavigation?.TenHang + v.MaGheNavigation?.SoThu));

                    htmlVe.Append($@"
                    <div style='border: 1px solid #0d6efd; border-radius: 8px; margin-bottom: 20px; overflow: hidden;'>
                        <div style='background-color: #0d6efd; color: white; padding: 10px 15px; font-weight: bold;'>THÔNG TIN VÉ XEM PHIM</div>
                        <div style='padding: 15px; background-color: #f8f9fa;'>
                            <h3 style='color: #0d6efd; margin-top: 0;'>{suat?.MaPhimNavigation?.TenPhim}</h3>
                            <p style='margin: 5px 0;'><strong>Suất chiếu:</strong> {suat?.ThoiGianBatDau.ToString("HH:mm - dd/MM/yyyy")}</p>
                            <p style='margin: 5px 0;'><strong>Phòng:</strong> {suat?.MaPhongNavigation?.TenPhong}</p>
                            <p style='margin: 5px 0;'><strong>Ghế đã chọn:</strong> <span style='color: #dc3545; font-weight: bold;'>{danhSachGhe}</span></p>
                        </div>
                    </div>");
                }

                // 2. Dựng HTML phần Bắp nước
                StringBuilder htmlBapNuoc = new StringBuilder();
                if (donHang.ChiTietDichVu != null && donHang.ChiTietDichVu.Any())
                {
                    htmlBapNuoc.Append(@"
                    <div style='border: 1px solid #ffc107; border-radius: 8px; margin-bottom: 20px; overflow: hidden;'>
                        <div style='background-color: #ffc107; color: #000; padding: 10px 15px; font-weight: bold;'>DỊCH VỤ ĐI KÈM</div>
                        <div style='padding: 15px; background-color: #fff;'>");

                    foreach (var item in donHang.ChiTietDichVu)
                    {
                        htmlBapNuoc.Append($@"
                            <div style='display: flex; justify-content: space-between; border-bottom: 1px solid #eee; padding: 8px 0;'>
                                <span>{item.MaDichVuNavigation?.TenDichVu} (x{item.SoLuong})</span>
                                <strong>{string.Format("{0:#,##0}", item.DonGia * item.SoLuong)}đ</strong>
                            </div>");
                    }
                    htmlBapNuoc.Append("</div></div>");
                }

                string giamGiaHtml = donHang.MaKhuyenMai != null ? $@"<div style='display: flex; justify-content: space-between; color: #28a745; margin-bottom: 5px;'><span>Giảm giá ({donHang.MaKhuyenMai}):</span><strong>-{string.Format("{0:#,##0}", donHang.TongTienBanDau - donHang.TongTienSauGiam)}đ</strong></div>" : "";
                // 3. Ráp toàn bộ Email (Đưa QR lên trên cùng)
                string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px; overflow: hidden;'>
                    <div style='background-color: #198754; color: white; text-align: center; padding: 20px;'>
                        <h2 style='margin: 0;'>THANH TOÁN THÀNH CÔNG</h2>
                        <p style='margin: 5px 0 0 0;'>Giao dịch #{donHang.MaDonHang}</p>
                    </div>
                    <div style='padding: 20px;'>
                        
                        <div style='text-align: center; margin-bottom: 25px; padding-bottom: 20px; border-bottom: 2px dashed #ddd;'>
                            <img src='https://api.qrserver.com/v1/create-qr-code/?size=180x180&data={donHang.MaDonHang}' alt='QR Code' style='border: 1px solid #ddd; padding: 10px; border-radius: 10px;' />
                            <p style='font-size: 14px; color: #555; margin-top: 10px; font-weight: bold;'>Quét mã này tại Kiosk để in vé và biên lai</p>
                        </div>

                        {htmlVe.ToString()}
                        {htmlBapNuoc.ToString()}
                        
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin-top: 20px;'>
                            <div style='display: flex; justify-content: space-between; margin-bottom: 5px;'>
                                <span>Tạm tính:</span><strong>{string.Format("{0:#,##0}", donHang.TongTienBanDau)}đ</strong>
                            </div>
                            {giamGiaHtml}
                            <div style='display: flex; justify-content: space-between; margin-top: 10px; border-top: 2px solid #ddd; padding-top: 10px;'>
                                <h3 style='margin: 0; color: #333;'>TỔNG THANH TOÁN:</h3>
                                <h3 style='margin: 0; color: #dc3545;'>{string.Format("{0:#,##0}", donHang.TongTienSauGiam)}đ</h3>
                            </div>
                        </div>
                    </div>
                </div>";

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = $"[B E T A Cinemas] Hóa đơn điện tử #{donHang.MaDonHang}",
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    await smtp.SendMailAsync(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        // ================================================
        // ĐĂNG KÝ BẰNG MẬT KHẨU
        // ================================================
        public async Task<(bool ThanhCong, string ThongBao, string? TenDangNhap)> DangKyAsync(DangKyViewModel model)
        {
            // K4: Kiểm tra email trùng
            bool emailTonTai = await _context.KhachHang.AnyAsync(k => k.Email == model.Email && !k.DaXoa);
            if (emailTonTai)
                return (false, "Email nay da duoc dang ky. Vui long dung email khac.", null);

            // Kiểm tra tên đăng nhập trùng
            bool tenTonTai = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == model.TenDangNhap && !t.DaXoa);
            if (tenTonTai)
                return (false, "Ten dang nhap da ton tai. Vui long chon ten khac.", null);

            // K6: Kiểm tra độ mạnh mật khẩu
            var (duManh, lyDo) = KiemTraDoManhMatKhau(model.MatKhau);
            if (!duManh)
                return (false, lyDo, null);

            // Sinh mã KhachHang tự động
            string maKH = await SinhMaKhachHangAsync();

            // Sinh token xác minh email (random 48 bytes, URL-safe)
            string token = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            VerifyTokenStore.Save(token, model.Email.Trim().ToLower(), model.TenDangNhap.Trim());

            var khachHang = new KhachHang
            {
                MaKhachHang = maKH,
                HoTen = model.HoTen.Trim(),
                Email = model.Email.Trim().ToLower(),
                MaLoaiKh = "LKH01",
                DiemTichLuy = 0,
                PhanTramGiamGia = 0,
                DaXoa = false
            };

            // K7: TrangThai = "ChoXacMinh" — chờ xác minh email
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap.Trim(),
                MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau),
                VaiTro = "KhachHang",
                TrangThai = "ChoXacMinh",
                MaKhachHang = maKH,
                DaXoa = false
            };

            using var tx = await _context.Database.BeginTransactionAsync();
            _context.KhachHang.Add(khachHang);
            _context.TaiKhoan.Add(taiKhoan);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, "OK", model.TenDangNhap.Trim());
        }

        // ================================================
        // GỬI EMAIL XÁC MINH (K8)
        // ================================================
        public async Task<bool> GuiEmailXacMinhAsync(string email, string tenDangNhap, string token)
        {
            string baseUrl = _config["AppUrl"] ?? "https://localhost:7000";
            // SAU (đúng — có đầy đủ area)
            // Sửa lại thành (không có NguoiDung)
            string link = $"{baseUrl}/TaiKhoan/XacMinhEmail"
                        + $"?token={Uri.EscapeDataString(token)}"
                        + $"&user={Uri.EscapeDataString(tenDangNhap)}";

            string body = "<div style='font-family:Segoe UI,sans-serif;max-width:520px;margin:auto;'>"
                + "<div style='background:#034ea2;padding:28px 32px;border-radius:12px 12px 0 0;text-align:center;'>"
                + "<h2 style='color:#fff;margin:0;'>Rap Chieu Phim Nhom 1</h2></div>"
                + "<div style='background:#fff;padding:32px;border:1px solid #e5e7eb;border-radius:0 0 12px 12px;'>"
                + $"<p>Xin chao <strong>{email}</strong>,</p>"
                + "<p>Nhan nut ben duoi de xac minh email va kich hoat tai khoan:</p>"
                + "<div style='text-align:center;margin:28px 0;'>"
                + $"<a href='{link}' style='background:#034ea2;color:#fff;padding:14px 32px;"
                + "border-radius:50px;text-decoration:none;font-weight:700;display:inline-block;'>"
                + "Xac minh tai khoan</a></div>"
                + "<p style='color:#6b7280;font-size:13px;'>Link co hieu luc trong 24 gio.</p>"
                + "</div></div>";

            return await GuiEmailAsync(email, "Xac minh tai khoan - Rap Chieu Phim Nhom 1", body);
        }

        // ================================================
        // XÁC MINH EMAIL (K10)
        // ================================================
        public async Task<(bool ThanhCong, string ThongBao)> XacMinhEmailAsync(string token, string tenDangNhap)
        {
            var info = VerifyTokenStore.Get(token);
            if (info == null)
                return (false, "Link xac minh khong hop le hoac da het han (24 gio). Vui long gui lai.");

            if (info.TenDangNhap != tenDangNhap)
                return (false, "Link xac minh khong hop le.");

            var tk = await _context.TaiKhoan
                .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap && !t.DaXoa);

            if (tk == null)
                return (false, "Khong tim thay tai khoan.");

            if (tk.TrangThai == "HoatDong")
                return (false, "Tai khoan da duoc xac minh. Vui long dang nhap.");

            tk.TrangThai = "HoatDong";
            _context.TaiKhoan.Update(tk);
            await _context.SaveChangesAsync();
            VerifyTokenStore.Remove(token);

            return (true, "Tai khoan da duoc kich hoat thanh cong! Vui long dang nhap.");
        }

        // ================================================
        // XỬ LÝ GOOGLE CALLBACK (G4 → G7)
        // ================================================
        public async Task<(TaiKhoan? TaiKhoan, bool LaKhachMoi, string ThongBao)> XuLyGoogleCallbackAsync(
            string email, string hoTen)
        {
            email = email.Trim().ToLower();
            hoTen = hoTen.Trim();

            // G5: Kiểm tra email đã tồn tại chưa
            var khCu = await _context.KhachHang
                .FirstOrDefaultAsync(k => k.Email == email && !k.DaXoa);

            if (khCu != null)
            {
                // G5.a: Email đã tồn tại → tìm tài khoản, đăng nhập luôn
                var tkCu = await _context.TaiKhoan
                    .Include(t => t.MaKhachHangNavigation)
                    .FirstOrDefaultAsync(t =>
                        t.MaKhachHang == khCu.MaKhachHang &&
                        t.TrangThai == "HoatDong" &&
                        !t.DaXoa);

                if (tkCu == null)
                    return (null, false, "Tai khoan dang bi khoa hoac cho xac minh.");

                return (tkCu, false, $"Chao mung tro lai, {hoTen}!");
            }

            // G6: Email chưa tồn tại → tạo mới, kích hoạt luôn (Google đã xác minh)
            string maKH = await SinhMaKhachHangAsync();
            string tenDN = await SinhTenDangNhapGoogleAsync(email);

            var khMoi = new KhachHang
            {
                MaKhachHang = maKH,
                HoTen = hoTen,
                Email = email,
                MaLoaiKh = "LKH01",
                DiemTichLuy = 0,
                PhanTramGiamGia = 0,
                DaXoa = false
            };

            var tkMoi = new TaiKhoan
            {
                TenDangNhap = tenDN,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                VaiTro = "KhachHang",
                TrangThai = "HoatDong",
                MaKhachHang = maKH,
                DaXoa = false
            };

            using var tx = await _context.Database.BeginTransactionAsync();
            _context.KhachHang.Add(khMoi);
            _context.TaiKhoan.Add(tkMoi);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            tkMoi.MaKhachHangNavigation = khMoi;
            return (tkMoi, true, $"Dang ky thanh cong! Chao mung {hoTen}!");
        }

        // ================================================
        // PRIVATE HELPERS (dùng nội bộ)
        // ================================================
        private static (bool DuManh, string LyDo) KiemTraDoManhMatKhau(string pw)
        {
            if (pw.Length < 8)
                return (false, "Mat khau phai co it nhat 8 ky tu.");
            if (!pw.Any(char.IsUpper))
                return (false, "Mat khau phai co it nhat 1 chu hoa (A-Z).");
            if (!pw.Any(char.IsLower))
                return (false, "Mat khau phai co it nhat 1 chu thuong (a-z).");
            if (!pw.Any(char.IsDigit))
                return (false, "Mat khau phai co it nhat 1 chu so (0-9).");
            if (!pw.Any(c => "!@#$%^&*()-_=+[]{}|;':\",./<>?".Contains(c)))
                return (false, "Mat khau phai co it nhat 1 ky tu dac biet (!@#$...).");
            return (true, "OK");
        }

        private async Task<string> SinhMaKhachHangAsync()
        {
            int count = await _context.KhachHang.CountAsync();
            string ma;
            do
            {
                count++;
                ma = $"KH{count:D2}";
            }
            while (await _context.KhachHang.AnyAsync(k => k.MaKhachHang == ma));
            return ma;
        }

        private async Task<string> SinhTenDangNhapGoogleAsync(string email)
        {
            string baseNam = email.Split('@')[0].Replace(".", "_");
            string tenDN;
            var rng = new Random();
            do
            {
                tenDN = $"{baseNam}_{rng.Next(100, 999)}";
            }
            while (await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == tenDN));
            return tenDN;
        }

        private async Task<bool> GuiEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var from = _config["Email:From"]!;
                var dispName = _config["Email:DisplayName"] ?? "Rap Chieu Phim";
                var password = _config["Email:Password"]!;
                var host = _config["Email:Host"] ?? "smtp.gmail.com";
                int port = _config.GetValue<int>("Email:Port", 587);

                using var smtp = new System.Net.Mail.SmtpClient
                {
                    Host = host,
                    Port = port,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(from, password)
                };
                using var msg = new System.Net.Mail.MailMessage(
                    new System.Net.Mail.MailAddress(from, dispName),
                    new System.Net.Mail.MailAddress(toEmail))
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                await smtp.SendMailAsync(msg);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    public static class VerifyTokenStore
    {
        public record TokenInfo(string Email, string TenDangNhap, DateTime Expiry);

        private static readonly Dictionary<string, TokenInfo> _store = new();
        private static readonly object _lock = new();

        public static void Save(string token, string email, string tenDangNhap)
        {
            lock (_lock)
            {
                var expired = _store
                    .Where(x => x.Value.Expiry < DateTime.UtcNow)
                    .Select(x => x.Key)
                    .ToList();
                expired.ForEach(k => _store.Remove(k));

                _store[token] = new TokenInfo(email, tenDangNhap, DateTime.UtcNow.AddHours(24));
            }
        }

        public static TokenInfo? Get(string token)
        {
            lock (_lock)
            {
                if (_store.TryGetValue(token, out var info) && info.Expiry >= DateTime.UtcNow)
                    return info;
                return null;
            }
        }

        public static string? GetTokenByUser(string tenDangNhap)
        {
            lock (_lock)
            {
                return _store
                    .FirstOrDefault(x =>
                        x.Value.TenDangNhap == tenDangNhap &&
                        x.Value.Expiry >= DateTime.UtcNow)
                    .Key;
            }
        }

        public static void Remove(string token)
        {
            lock (_lock) { _store.Remove(token); }
        }
    }
}
