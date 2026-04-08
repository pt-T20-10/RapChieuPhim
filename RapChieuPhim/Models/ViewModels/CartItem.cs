namespace RapChieuPhim.Models.ViewModels
{
    public class CartItem
    {
        public string MaDichVu { get; set; } = null!;
        public string TenDichVu { get; set; } = null!;
        public double GiaBan { get; set; }
        public int SoLuong { get; set; }
        public string DuongDanHinh { get; set; } = null!;
        public double ThanhTien => GiaBan * SoLuong;
    }
}