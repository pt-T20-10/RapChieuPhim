using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class Phim
{
    public string MaPhim { get; set; } = null!;

    public string TenPhim { get; set; } = null!;

    public string MaTheLoai { get; set; } = null!;

    public int ThoiLuong { get; set; }

    public DateOnly NgayPhatHanh { get; set; }

    public string? MoTa { get; set; }

    public string? PhanLoaiDoTuoi { get; set; }

    public string? DuongDanAnh { get; set; }

    public string TrangThai { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual TheLoaiPhim MaTheLoaiNavigation { get; set; } = null!;

    public virtual ICollection<SuatChieu> SuatChieus { get; set; } = new List<SuatChieu>();
}
