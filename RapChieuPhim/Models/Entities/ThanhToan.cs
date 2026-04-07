using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class ThanhToan
{
    public string MaThanhToan { get; set; } = null!;

    public string MaDonHang { get; set; } = null!;

    public string PhuongThuc { get; set; } = null!;

    public double SoTien { get; set; }

    public DateTime NgayThanhToan { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? MaGiaoDichNgoai { get; set; }

    public bool DaXoa { get; set; }

    public virtual DonHang MaDonHangNavigation { get; set; } = null!;
}
