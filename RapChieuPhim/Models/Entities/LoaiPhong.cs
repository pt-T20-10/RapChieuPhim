using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class LoaiPhong
{
    public string MaLoaiPhong { get; set; } = null!;

    public string TenLoaiPhong { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual ICollection<PhongChieu> PhongChieu { get; set; } = new List<PhongChieu>();
}
