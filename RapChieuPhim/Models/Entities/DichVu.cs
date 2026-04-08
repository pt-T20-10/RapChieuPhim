using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class DichVu
{
    public string MaDichVu { get; set; } = null!;

    public string TenDichVu { get; set; } = null!;

    public string MaDanhMuc { get; set; } = null!;

    public double GiaBan { get; set; }

    public int SoLuongTon { get; set; }

    public bool DaXoa { get; set; }

    public string? DuongDanHinh { get; set; }

    public virtual ICollection<ChiTietDichVu> ChiTietDichVu { get; set; } = new List<ChiTietDichVu>();

    public virtual DanhMucDichVu MaDanhMucNavigation { get; set; } = null!;
}
