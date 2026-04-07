using System;
using System.Collections.Generic;

namespace RapChieuPhim.Models.ViewModels
{
    public class ThongKeViewModel
    {
        // Bộ lọc thời gian
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }

        // Các chỉ số tổng quan
        public double TongDoanhThu { get; set; }
        public int TongSoDonHang { get; set; }
        public int TongSoVe { get; set; }

        // Dữ liệu cho biểu đồ cột (Doanh thu theo ngày)
        public List<DoanhThuNgayItem> DoanhThuTheoNgay { get; set; } = new();

        // Dữ liệu cho Bảng Top 5 phim doanh thu cao nhất
        public List<TopPhimItem> TopPhim { get; set; } = new();

        // Dữ liệu cho Bảng Tỷ lệ lấp đầy theo phòng
        public List<LapDayPhongItem> LapDayTheoPhong { get; set; } = new();
    }

    public class DoanhThuNgayItem
    {
        public string Ngay { get; set; } = "";       // Định dạng hiển thị "dd/MM"
        public double DoanhThu { get; set; }
        public int SoVe { get; set; }
    }

    public class TopPhimItem
    {
        public string TenPhim { get; set; } = "";
        public int SoVe { get; set; }
        public double DoanhThu { get; set; }
    }

    public class LapDayPhongItem
    {
        public string TenPhong { get; set; } = "";
        public int TongGhe { get; set; }
        public int GheDaDat { get; set; }

        // Tự động tính toán tỷ lệ % lấp đầy, làm tròn 1 chữ số thập phân
        public double TyLe => TongGhe == 0 ? 0 : Math.Round((double)GheDaDat / TongGhe * 100, 1);
    }

    public class DuBaoViewModel
    {
        public int SoNgayDuBao { get; set; } = 7; // Mặc định dự báo 7 ngày tới
        public double DoanhThuDuKien { get; set; }
        public bool DuDieuKien { get; set; } = true;
        public string ThongBao { get; set; } = "";

        // Dữ liệu để vẽ biểu đồ đường xu hướng
        public List<DuBaoItem> DanhSachDuBao { get; set; } = new();
    }

    public class DuBaoItem
    {
        public string Ngay { get; set; } = "";
        public double DoanhThu { get; set; }
        public bool LaDuBao { get; set; } // True: Dữ liệu dự đoán, False: Dữ liệu thực tế quá khứ
    }
}