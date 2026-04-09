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

    public int SoLuongVe { get; set; }

    public string TenPhim { get; set; } = null!;

    public string TenPhong { get; set; } = null!;

    public string MaGhe { get; set; } = null!;

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public double GiaVe { get; set; }

    public double GiaDichVu { get; set; }

    public double TongTien { get; set; }

    public string TrangThaiMoi { get; set; } = null!;

    public List<DichVuQuetResponse> DanhSachDichVu { get; set; } = new();
}

public class DichVuQuetResponse
{
    public string TenDichVu { get; set; } = null!;

    public int SoLuong { get; set; }

    public double DonGia { get; set; }

    public double ThanhTien { get; set; }
}