using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RapChieuPhim.Services
{
    public class ThongKeService
    {
        private readonly AppDbContext _context;

        public ThongKeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ThongKeViewModel> LayThongKeAsync(DateTime tuNgay, DateTime denNgay)
        {
            var viewModel = new ThongKeViewModel
            {
                TuNgay = tuNgay,
                DenNgay = denNgay
            };

            // Lọc vé chưa xóa ngay từ DB (Chuẩn Query của sếp)
            var donHangs = await _context.DonHangs
                .Include(d => d.ChiTietVes.Where(v => !v.DaXoa))
                    .ThenInclude(v => v.MaSuatChieuNavigation)
                        .ThenInclude(s => s.MaPhimNavigation)
                .Include(d => d.ChiTietVes.Where(v => !v.DaXoa))
                    .ThenInclude(v => v.MaSuatChieuNavigation)
                        .ThenInclude(s => s.MaPhongNavigation)
                .Where(d => d.TrangThai == "DaThanhToan"
                         && !d.DaXoa
                         && d.NgayTao.Date >= tuNgay.Date
                         && d.NgayTao.Date <= denNgay.Date)
                .AsNoTracking()
                .ToListAsync();

            viewModel.TongDoanhThu = donHangs.Sum(d => d.TongTienSauGiam);
            viewModel.TongSoDonHang = donHangs.Count;
            viewModel.TongSoVe = donHangs.Sum(d => d.ChiTietVes.Count);

            viewModel.DoanhThuTheoNgay = donHangs
                .GroupBy(d => d.NgayTao.Date)
                .Select(g => new DoanhThuNgayItem
                {
                    Ngay = g.Key.ToString("dd/MM"),
                    DoanhThu = g.Sum(d => d.TongTienSauGiam),
                    SoVe = g.Sum(d => d.ChiTietVes.Count)
                })
                .OrderBy(x => x.Ngay)
                .ToList();

            var tatCaVe = donHangs.SelectMany(d => d.ChiTietVes).ToList();

            viewModel.TopPhim = tatCaVe
                .Where(v => v.MaSuatChieuNavigation != null && v.MaSuatChieuNavigation.MaPhimNavigation != null)
                .GroupBy(v => v.MaSuatChieuNavigation.MaPhimNavigation.TenPhim)
                .Select(g => new TopPhimItem
                {
                    TenPhim = g.Key,
                    SoVe = g.Count(),
                    DoanhThu = g.Sum(v => v.GiaVe)
                })
                .OrderByDescending(x => x.DoanhThu)
                .Take(5)
                .ToList();

            var phongChieus = await _context.PhongChieus.Where(p => !p.DaXoa && p.TrangThai == "HoatDong").AsNoTracking().ToListAsync();

            var lapDayList = new List<LapDayPhongItem>();
            foreach (var phong in phongChieus)
            {
                var suatChieusCuaPhong = await _context.SuatChieus
                    .Where(s => s.MaPhong == phong.MaPhong && !s.DaXoa
                             && s.ThoiGianBatDau.Date >= tuNgay.Date
                             && s.ThoiGianBatDau.Date <= denNgay.Date)
                    .AsNoTracking()
                    .ToListAsync();

                int tongGhePhucVu = suatChieusCuaPhong.Count * phong.SoGhe;

                if (tongGhePhucVu > 0)
                {
                    int gheDaDat = tatCaVe.Count(v => v.MaSuatChieuNavigation?.MaPhong == phong.MaPhong);

                    lapDayList.Add(new LapDayPhongItem
                    {
                        TenPhong = phong.TenPhong,
                        TongGhe = tongGhePhucVu,
                        GheDaDat = gheDaDat
                    });
                }
            }
            viewModel.LapDayTheoPhong = lapDayList.OrderByDescending(x => x.TyLe).ToList();

            return viewModel;
        }

        // 3. Hàm xử lý Dự báo doanh thu
        public async Task<DuBaoViewModel> TinhDuBaoDoanhThuAsync(int soNgayDuBao)
        {
            var result = new DuBaoViewModel { SoNgayDuBao = soNgayDuBao };

            // Lấy dữ liệu thực tế 7 ngày gần nhất để làm căn cứ
            var ngayKetThuc = DateTime.Now.Date.AddDays(-1);
            var ngayBatDau = ngayKetThuc.AddDays(-6);

            var duLieuQuaKhu = await _context.DonHangs
                .Where(d => d.TrangThai == "DaThanhToan" && !d.DaXoa
                         && d.NgayTao.Date >= ngayBatDau && d.NgayTao.Date <= ngayKetThuc)
                .GroupBy(d => d.NgayTao.Date)
                .Select(g => new { Ngay = g.Key, DoanhThu = g.Sum(d => d.TongTienSauGiam) })
                .OrderBy(x => x.Ngay)
                .ToListAsync();

            // KIỂM TRA ĐIỀU KIỆN: Phải có đủ dữ liệu (theo đặc tả)
            if (duLieuQuaKhu.Count < 7)
            {
                result.DuDieuKien = false;
                result.ThongBao = "Dữ liệu lịch sử không đủ để chạy mô hình dự báo. Cần tối thiểu 7 ngày hoạt động.";
                return result;
            }

            // Đưa dữ liệu quá khứ vào danh sách hiển thị biểu đồ
            foreach (var item in duLieuQuaKhu)
            {
                result.DanhSachDuBao.Add(new DuBaoItem
                {
                    Ngay = item.Ngay.ToString("dd/MM"),
                    DoanhThu = item.DoanhThu,
                    LaDuBao = false
                });
            }

            // THUẬT TOÁN: Trung bình trượt đơn giản
            double trungBinhNgay = duLieuQuaKhu.Average(x => x.DoanhThu);
            result.DoanhThuDuKien = trungBinhNgay * soNgayDuBao;

            // Tạo dữ liệu giả lập cho các ngày tương lai trên biểu đồ
            for (int i = 1; i <= soNgayDuBao; i++)
            {
                result.DanhSachDuBao.Add(new DuBaoItem
                {
                    Ngay = DateTime.Now.Date.AddDays(i - 1).ToString("dd/MM"),
                    DoanhThu = Math.Round(trungBinhNgay, 0),
                    LaDuBao = true
                });
            }

            return result;
        }

        // ==========================================
        // FIX LỖI EPPLUS 8 BẰNG CÚ PHÁP MỚI NHẤT
        // ==========================================
        public byte[] XuatExcel(ThongKeViewModel data)
        {
            // Cú pháp BẮT BUỘC của EPPlus 8 trở lên: Phải gọi hàm SetNonCommercial... và truyền tên vào
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Dev Phan Trung Nghia");

            using var package = new OfficeOpenXml.ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DoanhThu");

            ws.Cells[1, 1].Value = "Ngày";
            ws.Cells[1, 2].Value = "Doanh thu (VNĐ)";
            ws.Cells[1, 3].Value = "Số vé bán ra";

            ws.Cells["A1:C1"].Style.Font.Bold = true;

            for (int i = 0; i < data.DoanhThuTheoNgay.Count; i++)
            {
                ws.Cells[i + 2, 1].Value = data.DoanhThuTheoNgay[i].Ngay;
                ws.Cells[i + 2, 2].Value = data.DoanhThuTheoNgay[i].DoanhThu;
                ws.Cells[i + 2, 3].Value = data.DoanhThuTheoNgay[i].SoVe;
            }

            ws.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}