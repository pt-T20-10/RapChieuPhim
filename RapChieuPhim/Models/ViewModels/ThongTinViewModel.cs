using System.ComponentModel.DataAnnotations;

namespace RapChieuPhim.Models.ViewModels
{
    public class ThongTinViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = null!;

        // Regex kiểm tra đúng 10 số, bắt đầu bằng số 0
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0.")]
        public string? SoDienThoai { get; set; }

        [TuoiToiThieu(15)]
        public DateOnly? NgaySinh { get; set; }

        public string? GioiTinh { get; set; }
    }

    // Custom Attribute để tính toán và kiểm tra tuổi
    public class TuoiToiThieuAttribute : ValidationAttribute
    {
        private readonly int _tuoi;

        public TuoiToiThieuAttribute(int tuoi)
        {
            _tuoi = tuoi;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateOnly ngaySinh)
            {
                // Lấy ngày hiện tại và trừ đi số tuổi quy định
                var today = DateOnly.FromDateTime(DateTime.Now);
                var ngayToiDaDuocPhep = today.AddYears(-_tuoi); // Ví dụ: Hôm nay 07/04/2026 -> Max: 07/04/2011

                // Nếu ngày sinh nhập vào LỚN HƠN ngày tối đa cho phép -> Báo lỗi
                if (ngaySinh > ngayToiDaDuocPhep)
                {
                    return new ValidationResult($"Khách hàng phải đủ {_tuoi} tuổi (Sinh trước ngày {ngayToiDaDuocPhep:dd/MM/yyyy}).");
                }
            }
            return ValidationResult.Success;
        }
    }
}