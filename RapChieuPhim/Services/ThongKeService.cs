using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using OfficeOpenXml.Style;

namespace RapChieuPhim.Services
{
    public class ThongKeService
    {
        private readonly AppDbContext _context;

        public ThongKeService(AppDbContext context)
        {
            _context = context;
        }

        // --- HÀM MỚI: Lấy danh sách Phim để làm bộ lọc ---
        public async Task<List<SelectListItem>> LayDanhSachPhimAsync()
        {
            return await _context.Phim.Where(p => !p.DaXoa)
                .Select(p => new SelectListItem { Value = p.MaPhim, Text = p.TenPhim })
                .ToListAsync();
        }

        // --- HÀM MỚI: Lấy danh sách Phòng chiếu để làm bộ lọc ---
        public async Task<List<SelectListItem>> LayDanhSachPhongAsync()
        {
            return await _context.PhongChieu.Where(p => !p.DaXoa)
                .Select(p => new SelectListItem { Value = p.MaPhong, Text = p.TenPhong })
                .ToListAsync();
        }


        public async Task<ThongKeViewModel> LayThongKeAsync(DateTime tuNgay, DateTime denNgay, string maPhim = null, string maPhong = null)
        {
            var viewModel = new ThongKeViewModel { TuNgay = tuNgay, DenNgay = denNgay };

            // 1. KÉO DỮ LIỆU TỪ DB LÊN RAM (Kéo thêm Thể loại và Loại ghế)
            var DonHang = await _context.DonHang
                .Include(d => d.ChiTietVe.Where(v => !v.DaXoa))
                    .ThenInclude(v => v.MaSuatChieuNavigation)
                        .ThenInclude(s => s.MaPhimNavigation)
                            .ThenInclude(p => p.MaTheLoaiNavigation) // Kéo Thể loại
                .Include(d => d.ChiTietVe.Where(v => !v.DaXoa))
                    .ThenInclude(v => v.MaSuatChieuNavigation)
                        .ThenInclude(s => s.MaPhongNavigation)
                .Include(d => d.ChiTietVe.Where(v => !v.DaXoa))
                    .ThenInclude(v => v.MaGheNavigation)
                        .ThenInclude(g => g.MaLoaiGheNavigation) // Kéo Loại ghế
                .Where(d => d.TrangThai == "DaThanhToan" && !d.DaXoa && d.NgayTao.Date >= tuNgay.Date && d.NgayTao.Date <= denNgay.Date)
                .AsNoTracking().ToListAsync();

            if (!string.IsNullOrEmpty(maPhim))
                DonHang = DonHang.Where(d => d.ChiTietVe.Any(v => v.MaSuatChieuNavigation?.MaPhim == maPhim)).ToList();
            if (!string.IsNullOrEmpty(maPhong))
                DonHang = DonHang.Where(d => d.ChiTietVe.Any(v => v.MaSuatChieuNavigation?.MaPhong == maPhong)).ToList();

            viewModel.TongDoanhThu = DonHang.Sum(d => d.TongTienSauGiam);
            viewModel.TongSoDonHang = DonHang.Count;
            viewModel.TongSoVe = DonHang.Sum(d => d.ChiTietVe.Count);

            viewModel.DoanhThuTheoNgay = DonHang
                .GroupBy(d => d.NgayTao.Date)
                .Select(g => new DoanhThuNgayItem
                {
                    Ngay = g.Key.ToString("dd/MM"),
                    DoanhThu = g.Sum(d => d.TongTienSauGiam),
                    SoVe = g.Sum(d => d.ChiTietVe.Count)
                }).OrderBy(x => x.Ngay).ToList();

            // 3. THUẬT TOÁN BÓC TÁCH & PHÂN BỔ TRÊN RAM
            var tatCaVeDaPhanBo = DonHang.SelectMany(d =>
            {
                double tyLeThucThu = d.TongTienBanDau > 0 ? (d.TongTienSauGiam / d.TongTienBanDau) : 1;
                return d.ChiTietVe.Select(v => new
                {
                    TenPhim = v.MaSuatChieuNavigation?.MaPhimNavigation?.TenPhim,
                    MaPhong = v.MaSuatChieuNavigation?.MaPhong,
                    TenTheLoai = v.MaSuatChieuNavigation?.MaPhimNavigation?.MaTheLoaiNavigation?.TenTheLoai ?? "Khác",
                    LoaiGhe = v.MaGheNavigation?.MaLoaiGheNavigation?.TenLoaiGhe ?? "Thường",
                    GiaThucTe = v.GiaVe * tyLeThucThu
                });
            }).ToList();

            // Tính toán Top Phim, Đếm vé, Gom nhóm Thể loại
            viewModel.SoVeThuong = tatCaVeDaPhanBo.Count(v => v.LoaiGhe.ToLower().Contains("thường"));
            viewModel.SoVeVIP = tatCaVeDaPhanBo.Count(v => v.LoaiGhe.ToLower().Contains("vip"));

            viewModel.DoanhThuTheoTheLoai = tatCaVeDaPhanBo.GroupBy(v => v.TenTheLoai)
                .Select(g => new DoanhThuTheLoaiItem { TenTheLoai = g.Key, DoanhThu = Math.Round(g.Sum(v => v.GiaThucTe), 0) })
                .OrderByDescending(x => x.DoanhThu).ToList();

            viewModel.TopPhim = tatCaVeDaPhanBo.Where(v => !string.IsNullOrEmpty(v.TenPhim)).GroupBy(v => v.TenPhim)
                .Select(g => new TopPhimItem { TenPhim = g.Key, SoVe = g.Count(), DoanhThu = Math.Round(g.Sum(v => v.GiaThucTe), 0) })
                .OrderByDescending(x => x.DoanhThu).Take(5).ToList();

            // 4. Tính Tỷ lệ lấp đầy
            var PhongChieu = await _context.PhongChieu.Where(p => !p.DaXoa && p.TrangThai == "HoatDong").AsNoTracking().ToListAsync();
            var lapDayList = new List<LapDayPhongItem>();
            foreach (var phong in PhongChieu)
            {
                var SuatChieuCuaPhong = await _context.SuatChieu
                    .Where(s => s.MaPhong == phong.MaPhong && !s.DaXoa && s.ThoiGianBatDau.Date >= tuNgay.Date && s.ThoiGianBatDau.Date <= denNgay.Date)
                    .AsNoTracking().ToListAsync();

                int tongGhePhucVu = SuatChieuCuaPhong.Count * phong.SoGhe;
                if (tongGhePhucVu > 0)
                {
                    int gheDaDat = tatCaVeDaPhanBo.Count(v => v.MaPhong == phong.MaPhong);
                    lapDayList.Add(new LapDayPhongItem { TenPhong = phong.TenPhong, TongGhe = tongGhePhucVu, GheDaDat = gheDaDat });
                }
            }
            viewModel.LapDayTheoPhong = lapDayList.OrderByDescending(x => x.TyLe).ToList();

            return viewModel;
        }

        public async Task<DuBaoViewModel> TinhDuBaoDoanhThuAsync(int soNgayDuBao)
        {
            var result = new DuBaoViewModel { SoNgayDuBao = soNgayDuBao };
            var ngayKetThuc = DateTime.Now.Date.AddDays(-1);
            var ngayBatDau = ngayKetThuc.AddDays(-6);

            var duLieuQuaKhu = await _context.DonHang
                .Where(d => d.TrangThai == "DaThanhToan" && !d.DaXoa
                         && d.NgayTao.Date >= ngayBatDau && d.NgayTao.Date <= ngayKetThuc)
                .GroupBy(d => d.NgayTao.Date)
                .Select(g => new { Ngay = g.Key, DoanhThu = g.Sum(d => d.TongTienSauGiam) })
                .OrderBy(x => x.Ngay)
                .ToListAsync();

            if (duLieuQuaKhu.Count < 7)
            {
                result.DuDieuKien = false;
                result.ThongBao = "Dữ liệu lịch sử không đủ để chạy mô hình dự báo. Cần tối thiểu 7 ngày hoạt động.";
                return result;
            }

            foreach (var item in duLieuQuaKhu)
            {
                result.DanhSachDuBao.Add(new DuBaoItem { Ngay = item.Ngay.ToString("dd/MM"), DoanhThu = item.DoanhThu, LaDuBao = false });
            }

            // =========================================================================
            // THUẬT TOÁN MỚI: WMA (Trung bình trượt có trọng số) + Phân tích xu hướng
            // =========================================================================

            // 1. Tính trung bình có trọng số (Ưu tiên ngày gần nhất sẽ có sức ảnh hưởng cao hơn)
            double tongTrongSo = 0;
            double tongDoanhThuCoTrongSo = 0;
            for (int i = 0; i < duLieuQuaKhu.Count; i++)
            {
                int trongSo = i + 1; // Trọng số từ 1 đến 7
                tongTrongSo += trongSo;
                tongDoanhThuCoTrongSo += duLieuQuaKhu[i].DoanhThu * trongSo;
            }
            double trungBinhCoTrongSo = tongDoanhThuCoTrongSo / tongTrongSo;

            // 2. Tính hệ số xu hướng (Trend Factor) dựa trên sự biến động đầu-cuối
            double heSoXuHuong = duLieuQuaKhu.First().DoanhThu > 0
                ? (duLieuQuaKhu.Last().DoanhThu - duLieuQuaKhu.First().DoanhThu) / duLieuQuaKhu.First().DoanhThu / 7.0
                : 0;

            // Giới hạn xu hướng (Khóa chặn biên độ max 5% tăng/giảm mỗi ngày để không bị ảo)
            heSoXuHuong = Math.Clamp(heSoXuHuong, -0.05, 0.05);

            double doanhThuNgayNenTang = trungBinhCoTrongSo;
            double tongDuBao = 0;
            Random rand = new Random(); // Sinh số ngẫu nhiên tạo độ nhiễu thực tế

            // 3. Nội suy tương lai
            for (int i = 1; i <= soNgayDuBao; i++)
            {
                // Biên độ dao động ngẫu nhiên (-2% đến +2%) giúp biểu đồ có hình răng cưa tự nhiên
                double daoDongNganh = (rand.NextDouble() * 0.04) - 0.02;
                double doanhThuHomNay = doanhThuNgayNenTang * (1 + heSoXuHuong + daoDongNganh);

                result.DanhSachDuBao.Add(new DuBaoItem
                {
                    Ngay = DateTime.Now.Date.AddDays(i - 1).ToString("dd/MM"),
                    DoanhThu = Math.Round(doanhThuHomNay, 0),
                    LaDuBao = true
                });

                tongDuBao += doanhThuHomNay;
                doanhThuNgayNenTang = doanhThuHomNay; // Lấy doanh thu nay làm vốn cho ngày mai
            }

            result.DoanhThuDuKien = Math.Round(tongDuBao, 0);

            // 4. Sinh 3 kịch bản
            result.KichBanLacQuan = Math.Round(tongDuBao * 1.15, 0);
            result.KichBanBiQuan = Math.Round(tongDuBao * 0.85, 0);

            return result;
        }

        public byte[] XuatExcel(ThongKeViewModel data)
        {
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Dev Phan Trung Nghia");
            using var package = new OfficeOpenXml.ExcelPackage();

            // --- SHEET 1: TỔNG QUAN ---
            var ws1 = package.Workbook.Worksheets.Add("TongQuan_DoanhThu");
            ws1.Cells["A1:D1"].Merge = true;
            ws1.Cells["A1"].Value = "BÁO CÁO THỐNG KÊ DOANH THU RẠP CHIẾU PHIM";
            ws1.Cells["A1"].Style.Font.Bold = true;
            ws1.Cells["A1"].Style.Font.Size = 14;
            ws1.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws1.Cells["A2:D2"].Merge = true;
            ws1.Cells["A2"].Value = $"Kỳ báo cáo: Từ {data.TuNgay:dd/MM/yyyy} đến {data.DenNgay:dd/MM/yyyy}";
            ws1.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells["A2"].Style.Font.Italic = true;

            string[] headers = { "STT", "Ngày", "Số vé bán ra", "Doanh thu thực tế (VNĐ)" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws1.Cells[4, i + 1].Value = headers[i];
                ws1.Cells[4, i + 1].Style.Font.Bold = true;
                ws1.Cells[4, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws1.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(41, 128, 185)); // Xanh dương
                ws1.Cells[4, i + 1].Style.Font.Color.SetColor(Color.White);
            }

            int row = 5;
            foreach (var item in data.DoanhThuTheoNgay)
            {
                ws1.Cells[row, 1].Value = row - 4;
                ws1.Cells[row, 2].Value = item.Ngay;
                ws1.Cells[row, 3].Value = item.SoVe;
                ws1.Cells[row, 4].Value = item.DoanhThu;
                ws1.Cells[row, 4].Style.Numberformat.Format = "#,##0 ₫";
                row++;
            }
            ws1.Cells[row, 2].Value = "TỔNG CỘNG:";
            ws1.Cells[row, 2].Style.Font.Bold = true;
            ws1.Cells[row, 3].Value = data.TongSoVe;
            ws1.Cells[row, 4].Value = data.TongDoanhThu;
            ws1.Cells[row, 4].Style.Font.Bold = true;
            ws1.Cells[row, 4].Style.Numberformat.Format = "#,##0 ₫";
            ws1.Cells[$"A4:D{row}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            ws1.Cells.AutoFitColumns();

            // --- SHEET 2: TOP PHIM & THỂ LOẠI ---
            var ws2 = package.Workbook.Worksheets.Add("ChiTiet_Phim");
            ws2.Cells["A1:D1"].Merge = true;
            ws2.Cells["A1"].Value = "BẢNG XẾP HẠNG TOP PHIM";
            ws2.Cells["A1"].Style.Font.Bold = true;

            string[] headers2 = { "Xếp hạng", "Tên Phim", "Số lượng vé", "Doanh thu (VNĐ)" };
            for (int i = 0; i < headers2.Length; i++)
            {
                ws2.Cells[3, i + 1].Value = headers2[i];
                ws2.Cells[3, i + 1].Style.Font.Bold = true;
                ws2.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws2.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(39, 174, 96)); // Xanh lá
                ws2.Cells[3, i + 1].Style.Font.Color.SetColor(Color.White);
            }

            int row2 = 4;
            foreach (var item in data.TopPhim)
            {
                ws2.Cells[row2, 1].Value = "Top " + (row2 - 3);
                ws2.Cells[row2, 2].Value = item.TenPhim;
                ws2.Cells[row2, 3].Value = item.SoVe;
                ws2.Cells[row2, 4].Value = item.DoanhThu;
                ws2.Cells[row2, 4].Style.Numberformat.Format = "#,##0 ₫";
                row2++;
            }
            ws2.Cells[$"A3:D{row2 - 1}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            ws2.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        // ==========================================
        // THÊM MỚI: XUẤT EXCEL CHO PHẦN DỰ BÁO
        // ==========================================
        public byte[] XuatExcelDuBao(DuBaoViewModel data)
        {
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Dev Phan Trung Nghia");
            using var package = new OfficeOpenXml.ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DuBaoDoanhThu");

            ws.Cells[1, 1].Value = "Ngày";
            ws.Cells[1, 2].Value = "Doanh thu (VNĐ)";
            ws.Cells[1, 3].Value = "Loại dữ liệu"; // Phân biệt Thực tế hay Dự báo
            ws.Cells["A1:C1"].Style.Font.Bold = true;

            for (int i = 0; i < data.DanhSachDuBao.Count; i++)
            {
                ws.Cells[i + 2, 1].Value = data.DanhSachDuBao[i].Ngay;
                ws.Cells[i + 2, 2].Value = data.DanhSachDuBao[i].DoanhThu;
                ws.Cells[i + 2, 3].Value = data.DanhSachDuBao[i].LaDuBao ? "Dự báo kỳ vọng" : "Thực tế quá khứ";
            }

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }
    }
}