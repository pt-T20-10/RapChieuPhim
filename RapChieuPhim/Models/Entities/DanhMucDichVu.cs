using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.Entities;

public partial class DanhMucDichVu
{
    public string MaDanhMuc { get; set; } = null!;

    public string TenDanhMuc { get; set; } = null!;

    public bool DaXoa { get; set; }

    public virtual ICollection<DichVu> DichVus { get; set; } = new List<DichVu>();
}
