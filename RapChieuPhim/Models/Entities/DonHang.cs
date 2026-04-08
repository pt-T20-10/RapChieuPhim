using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class DonHang
{
    public string MaDonHang { get; set; } = null!;

    public string? MaKhachHang { get; set; }

    public string? MaNhanVien { get; set; }

    public string? MaKhuyenMai { get; set; }

    public DateTime NgayTao { get; set; }

    public double TongTienBanDau { get; set; }

    public double TongTienSauGiam { get; set; }

    public string TrangThai { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual ICollection<ChiTietDichVu> ChiTietDichVu { get; set; } = new List<ChiTietDichVu>();

    public virtual ICollection<ChiTietVe> ChiTietVe { get; set; } = new List<ChiTietVe>();

    public virtual KhachHang? MaKhachHangNavigation { get; set; }

    public virtual KhuyenMai? MaKhuyenMaiNavigation { get; set; }

    public virtual NhanVien? MaNhanVienNavigation { get; set; }

    public virtual ICollection<ThanhToan> ThanhToan { get; set; } = new List<ThanhToan>();
}
