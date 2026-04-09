using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class LoaiGhe
{
    public string MaLoaiGhe { get; set; } = null!;

    public string TenLoaiGhe { get; set; } = null!;

    public double HeSoGia { get; set; }

    public bool DaXoa { get; set; }

    public virtual ICollection<Ghe> Ghe { get; set; } = new List<Ghe>();
}
