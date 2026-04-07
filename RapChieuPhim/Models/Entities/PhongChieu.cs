using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class PhongChieu
{
    public string MaPhong { get; set; } = null!;

    public string TenPhong { get; set; } = null!;

    public string MaLoaiPhong { get; set; } = null!;

    public int SoGhe { get; set; }

    public string TrangThai { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual ICollection<Ghe> Ghes { get; set; } = new List<Ghe>();

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;

    public virtual ICollection<SuatChieu> SuatChieus { get; set; } = new List<SuatChieu>();
}
