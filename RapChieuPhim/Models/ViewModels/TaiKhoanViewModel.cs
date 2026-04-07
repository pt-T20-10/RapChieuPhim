using System.ComponentModel.DataAnnotations;

namespace RapChieuPhim.Models.ViewModels
{
    public class DangNhapViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email.")]
        [Display(Name = "Tên đăng nhập / Email")]
        public string TenDangNhap { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = null!;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool GhiNho { get; set; }
    }

    public class DangKyViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public string TenDangNhap { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string MatKhau { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string XacNhanMatKhau { get; set; } = null!;
    }
}