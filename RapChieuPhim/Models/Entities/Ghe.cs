using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class Ghe
{
    public string MaGhe { get; set; } = null!;

    public string MaPhong { get; set; } = null!;

    public string TenHang { get; set; } = null!;

    public string SoThu { get; set; } = null!;

    public string MaLoaiGhe { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual ICollection<ChiTietVe> ChiTietVe { get; set; } = new List<ChiTietVe>();

    public virtual LoaiGhe MaLoaiGheNavigation { get; set; } = null!;

    public virtual PhongChieu MaPhongNavigation { get; set; } = null!;
}
