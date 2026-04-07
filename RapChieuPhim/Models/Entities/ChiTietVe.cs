using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class ChiTietVe
{
    public string MaVe { get; set; } = null!;

    public string MaDonHang { get; set; } = null!;

    public string MaSuatChieu { get; set; } = null!;

    public string MaGhe { get; set; } = null!;

    public double GiaVe { get; set; }

    public string? MaQr { get; set; }

    public string TrangThai { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual DonHang MaDonHangNavigation { get; set; } = null!;

    public virtual Ghe MaGheNavigation { get; set; } = null!;

    public virtual SuatChieu MaSuatChieuNavigation { get; set; } = null!;
}
