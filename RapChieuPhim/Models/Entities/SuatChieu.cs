using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class SuatChieu
{
    public string MaSuatChieu { get; set; } = null!;

    public string MaPhim { get; set; } = null!;

    public string MaPhong { get; set; } = null!;

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public double GiaGoc { get; set; }

    public string TrangThai { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual ICollection<ChiTietVe> ChiTietVes { get; set; } = new List<ChiTietVe>();

    public virtual Phim MaPhimNavigation { get; set; } = null!;

    public virtual PhongChieu MaPhongNavigation { get; set; } = null!;
}
