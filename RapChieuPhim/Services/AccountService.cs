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

        public async Task<bool> GuiEmailHoaDonAsync(DonHang donHang, string toEmail)
        {
            try
            {
                var fromAddress = new MailAddress("nhnghia0501@gmail.com", "B E T A Cinemas");
                var toAddress = new MailAddress(toEmail);
                const string fromPassword = "xxxx xxxx xxxx xxxx"; // Mật khẩu ứng dụng của bạn

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                // 1. Dựng HTML phần Vé Phim
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
                            <div style='text-align: center; margin-top: 15px; border-top: 1px dashed #ccc; padding-top: 15px;'>
                                <img src='https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={donHang.MaDonHang}' alt='QR Code' />
                                <p style='font-size: 12px; color: #666;'>Mã quét vé tại rạp</p>
                            </div>
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

                // 3. Ráp toàn bộ Email
                string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px; overflow: hidden;'>
                    <div style='background-color: #198754; color: white; text-align: center; padding: 20px;'>
                        <h2 style='margin: 0;'>THANH TOÁN THÀNH CÔNG</h2>
                        <p style='margin: 5px 0 0 0;'>Giao dịch #{donHang.MaDonHang}</p>
                    </div>
                    <div style='padding: 20px;'>
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
    }
}