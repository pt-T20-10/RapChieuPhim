using RapChieuPhim.Models.Entities;

namespace RapChieuPhim.Models.ViewModels
{
    /// Bọc ChiTietVe + navigation properties đã load sẵn
    /// Dùng cho: hiển thị vé xem phim (giống vé CGV)
    public class VeViewModel
    {
        public ChiTietVe Ve { get; set; } = null!;

        // Shortcut tiện dùng trong View — đọc từ navigation
        public string TenPhim
            => Ve.MaSuatChieuNavigation?.MaPhimNavigation?.TenPhim ?? "—";

        public string PhanLoaiDoTuoi
            => Ve.MaSuatChieuNavigation?.MaPhimNavigation?.PhanLoaiDoTuoi ?? "";

        public DateTime ThoiGianBatDau
            => Ve.MaSuatChieuNavigation?.ThoiGianBatDau ?? default;

        public DateTime ThoiGianKetThuc
            => Ve.MaSuatChieuNavigation?.ThoiGianKetThuc ?? default;

        public string TenPhong
            => Ve.MaSuatChieuNavigation?.MaPhongNavigation?.TenPhong ?? "—";

        public string Ghe
            => $"{Ve.MaGheNavigation?.TenHang}{Ve.MaGheNavigation?.SoThu}";

        public string LoaiGhe
            => Ve.MaGheNavigation?.MaLoaiGheNavigation?.TenLoaiGhe ?? "—";
    }
}