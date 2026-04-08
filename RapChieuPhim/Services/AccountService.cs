using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;
using RapChieuPhim.Models.ViewModels;
using System.Net;
using System.Net.Mail;

namespace RapChieuPhim.Services
{
    public class AccountService
    {
        private readonly AppDbContext _context;

        public AccountService(AppDbContext context)
        {
            _context = context;
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
                const string fromPassword = "ugjx sbps gcih arte"; 

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
    }
}