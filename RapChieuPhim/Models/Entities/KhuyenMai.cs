using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class KhuyenMai
{
    public string MaKhuyenMai { get; set; } = null!;

    public string MaCode { get; set; } = null!;

    public double PhanTramGiam { get; set; }

    public double? GiamToiDa { get; set; }

    public DateTime TuNgay { get; set; }

    public DateTime DenNgay { get; set; }

    public int SoLuongConLai { get; set; }

    public double DonToiThieu { get; set; }

    public string TrangThai { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual ICollection<DonHang> DonHang { get; set; } = new List<DonHang>();
}
