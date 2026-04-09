namespace RapChieuPhim.Models.Results;

public class KetQuaQuetVe
{
    public bool ThanhCong { get; set; }

    public string MaTinhTrang { get; set; } = null!;

    public string TinNhan { get; set; } = null!;

    public ChiTietVeQuetResponse? DuLieuVe { get; set; }
}

public class ChiTietVeQuetResponse
{
    public string MaVe { get; set; } = null!;

    public string TenPhim { get; set; } = null!;

    public string TenPhong { get; set; } = null!;

    public string MaGhe { get; set; } = null!;

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public double GiaVe { get; set; }

    public string TrangThaiMoi { get; set; } = null!;
}