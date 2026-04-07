using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class TheLoaiPhim
{
    public string MaTheLoai { get; set; } = null!;

    public string TenTheLoai { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual ICollection<Phim> Phims { get; set; } = new List<Phim>();
}
