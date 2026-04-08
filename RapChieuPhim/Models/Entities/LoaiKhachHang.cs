using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class LoaiKhachHang
{
    public string MaLoaiKh { get; set; } = null!;

    public string TenLoaiKh { get; set; } = null!;

    public int NguongDiem { get; set; }

    public double PhanTramGiamGia { get; set; }

    public bool DaXoa { get; set; }

    public virtual ICollection<KhachHang> KhachHang { get; set; } = new List<KhachHang>();
}
