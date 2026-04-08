using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class NhanVien
{
    public string MaNhanVien { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public string? DiaChi { get; set; }

    public DateOnly NgayVaoLam { get; set; }

    public string? CaLamViec { get; set; }

    public bool DaXoa { get; set; }

    public virtual ICollection<DonHang> DonHang { get; set; } = new List<DonHang>();

    public virtual ICollection<TaiKhoan> TaiKhoan { get; set; } = new List<TaiKhoan>();
}
