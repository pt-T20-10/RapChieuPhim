using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class ChiTietDichVu
{
    public string MaChiTiet { get; set; } = null!;

    public string MaDonHang { get; set; } = null!;

    public string MaDichVu { get; set; } = null!;

    public int SoLuong { get; set; }

    public double DonGia { get; set; }

    public double ThanhTien { get; set; }

    public bool DaXoa { get; set; }

    public virtual DichVu MaDichVuNavigation { get; set; } = null!;

    public virtual DonHang MaDonHangNavigation { get; set; } = null!;
}
