using RapChieuPhim.Models.Entities;

namespace RapChieuPhim.Models.ViewModels
{
    /// Bọc DonHang + toàn bộ navigation đã load sẵn
    /// Dùng cho: hiển thị hóa đơn (vé + dịch vụ)
    public class HoaDonViewModel
    {
        public DonHang DonHang { get; set; } = null!;

        // Shortcut
        public string TenKhachHang
            => DonHang.MaKhachHangNavigation?.HoTen ?? "Khách vãng lai";

        public string SoDienThoai
            => DonHang.MaKhachHangNavigation?.SoDienThoai ?? "—";

        public string TenNhanVien
            => DonHang.MaNhanVienNavigation?.HoTen ?? "—";

        public string MaKhuyenMai
            => DonHang.MaKhuyenMaiNavigation?.MaCode ?? "";

        public double SoTienGiam
            => DonHang.TongTienBanDau - DonHang.TongTienSauGiam;

        public string PhuongThuc
            => DonHang.ThanhToan.FirstOrDefault()?.PhuongThuc ?? "—";

        // Danh sách vé — đọc thẳng từ navigation
        public IEnumerable<ChiTietVe> DanhSachVe
            => DonHang.ChiTietVe.Where(v => !v.DaXoa);

        // Danh sách dịch vụ — đọc thẳng từ navigation
        public IEnumerable<ChiTietDichVu> DanhSachDichVu
            => DonHang.ChiTietDichVu.Where(d => !d.DaXoa);

        public bool CoVe => DanhSachVe.Any();
        public bool CoDichVu => DanhSachDichVu.Any();
    }
}