using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class KhachHang
{
    public string MaKhachHang { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public string MaLoaiKh { get; set; } = null!;

    public int DiemTichLuy { get; set; }

    public string? HangThanhVien { get; set; }

    public double PhanTramGiamGia { get; set; }

    public bool DaXoa { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    public virtual LoaiKhachHang MaLoaiKhNavigation { get; set; } = null!;

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
