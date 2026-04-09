using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Data;
using RapChieuPhim.Models.Entities;

namespace RapChieuPhim.Services
{
    public class PdfTicketService
    {
        private readonly AppDbContext _dbContext;
        private readonly QRCodeService _qrCodeService;
        private readonly BaseFont _unicodeBaseFont;

        public PdfTicketService(AppDbContext dbContext, QRCodeService qrCodeService)
        {
            _dbContext = dbContext;
            _qrCodeService = qrCodeService;
            _unicodeBaseFont = LoadUnicodeBaseFont();
        }

        private static BaseFont LoadUnicodeBaseFont()
        {
            var candidates = new[]
            {
                @"C:\Windows\Fonts\arial.ttf",
                @"C:\Windows\Fonts\tahoma.ttf",
                @"C:\Windows\Fonts\times.ttf"
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    return BaseFont.CreateFont(path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                }
            }

            throw new FileNotFoundException("Không tìm thấy font Unicode hỗ trợ tiếng Việt.");
        }

        private Font F(float size, int style = Font.NORMAL)
        {
            return new Font(_unicodeBaseFont, size, style, BaseColor.BLACK);
        }

        public async Task<byte[]> GeneratePdfTicketAsync(string maVe)
        {
            try
            {
                var ve = await _dbContext.ChiTietVe
                    .Include(v => v.MaDonHangNavigation)
                        .ThenInclude(d => d.MaKhachHangNavigation)
                    .Include(v => v.MaSuatChieuNavigation)
                        .ThenInclude(sc => sc.MaPhimNavigation)
                    .Include(v => v.MaSuatChieuNavigation)
                        .ThenInclude(sc => sc.MaPhongNavigation)
                    .Include(v => v.MaGheNavigation)
                        .ThenInclude(g => g.MaLoaiGheNavigation)
                    .FirstOrDefaultAsync(v => v.MaVe == maVe);

                if (ve == null)
                    throw new Exception($"Vé {maVe} không tồn tại");

                var dichVuDiKem = await _dbContext.ChiTietDichVu
                    .Include(dv => dv.MaDichVuNavigation)
                    .Where(dv => dv.MaDonHang == ve.MaDonHang && !dv.DaXoa)
                    .ToListAsync();

                var document = new Document(PageSize.A4, 20, 20, 20, 20);
                var memoryStream = new MemoryStream();
                var writer = PdfWriter.GetInstance(document, memoryStream);

                document.Open();

                AddHeader(document);
                // BỎ QR để vé gọn như vé phim thực tế
                // AddQrCode(document, ve.MaQr ?? ve.MaVe);
                // document.Add(new Paragraph("\n"));

                AddScreeningInfo(document, ve);
                AddSeatInfo(document, ve);

                if (dichVuDiKem.Any())
                {
                    AddServiceInfo(document, dichVuDiKem);
                }

                AddPriceInfo(document, ve, dichVuDiKem);
                AddCustomerInfo(document, ve);
                AddRules(document);
                AddFooter(document);

                document.Close();

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tạo PDF vé: {ex.Message}");
            }
        }

        private void AddHeader(Document document)
        {
            var titleFont = F(14, Font.BOLD);
            var noteFont = F(8);

            document.Add(new Paragraph("VÉ XEM PHIM", titleFont) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("RẠP CHIẾU PHIM - PHIẾU XÁC NHẬN", noteFont) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph("\n"));

            var divider = new PdfPTable(1) { WidthPercentage = 100 };
            divider.AddCell(new PdfPCell(new Phrase(""))
            {
                BorderWidth = 0,
                Border = Rectangle.BOTTOM_BORDER,
                BorderColor = new BaseColor(120, 120, 120)
            });
            document.Add(divider);
            document.Add(new Paragraph("\n"));
        }

        private void AddQrCode(Document document, string maQr)
        {
            try
            {
                var qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(maQr);
                var base64Data = qrCodeBase64.Split(',')[1];
                var imageBytes = Convert.FromBase64String(base64Data);

                var image = Image.GetInstance(imageBytes);
                image.ScaleToFit(120f, 120f);
                image.Alignment = Element.ALIGN_CENTER;
                document.Add(image);

                var qrText = new Paragraph($"Mã QR: {maQr}", FontFactory.GetFont(FontFactory.HELVETICA, 9))
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(qrText);

                document.Add(new Paragraph("\n"));
            }
            catch (Exception ex)
            {
                var errorText = new Paragraph($"[Lỗi tạo QR: {ex.Message}]", FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.RED))
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(errorText);
                document.Add(new Paragraph("\n"));
            }
        }

        private void AddScreeningInfo(Document document, ChiTietVe ve)
        {
            var titleFont = F(10, Font.BOLD);
            var contentFont = F(9);
            var labelFont = F(9, Font.BOLD);

            document.Add(new Paragraph("THÔNG TIN SUẤT CHIẾU", titleFont));

            var table = new PdfPTable(2) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 40, 60 });

            void AddRow(string label, string value, bool underline = true)
            {
                table.AddCell(new PdfPCell(new Phrase(label, labelFont))
                {
                    BorderWidth = 0,
                    Padding = 5
                });

                table.AddCell(new PdfPCell(new Phrase(value, contentFont))
                {
                    BorderWidth = 0,
                    Padding = 5,
                    Border = underline ? Rectangle.BOTTOM_BORDER : Rectangle.NO_BORDER,
                    BorderColor = new BaseColor(180, 180, 180)
                });
            }

            AddRow("Tên phim:", ve.MaSuatChieuNavigation?.MaPhimNavigation?.TenPhim ?? "N/A");
            AddRow("Ngày chiếu:", ve.MaSuatChieuNavigation?.ThoiGianBatDau.ToString("dd/MM/yyyy") ?? "N/A");
            AddRow("Giờ chiếu:", $"{ve.MaSuatChieuNavigation?.ThoiGianBatDau:HH:mm} - {ve.MaSuatChieuNavigation?.ThoiGianKetThuc:HH:mm}");
            AddRow("Phòng:", ve.MaSuatChieuNavigation?.MaPhongNavigation?.TenPhong ?? "N/A", underline: false);

            document.Add(table);
            document.Add(new Paragraph("\n"));
        }

        private void AddSeatInfo(Document document, ChiTietVe ve)
        {
            var titleFont = F(10, Font.BOLD);
            var contentFont = F(9);
            var labelFont = F(9, Font.BOLD);

            document.Add(new Paragraph("THÔNG TIN VÉ", titleFont));

            var table = new PdfPTable(2) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 40, 60 });

            void AddRow(string label, string value, bool underline = true)
            {
                table.AddCell(new PdfPCell(new Phrase(label, labelFont))
                {
                    BorderWidth = 0,
                    Padding = 5
                });

                table.AddCell(new PdfPCell(new Phrase(value, contentFont))
                {
                    BorderWidth = 0,
                    Padding = 5,
                    Border = underline ? Rectangle.BOTTOM_BORDER : Rectangle.NO_BORDER,
                    BorderColor = new BaseColor(180, 180, 180)
                });
            }

            var seatType = ve.MaGheNavigation?.MaLoaiGheNavigation?.TenLoaiGhe ?? "Thường";
            AddRow("Ghế:", $"{ve.MaGheNavigation?.MaGhe} ({seatType})");
            AddRow("Mã vé:", ve.MaVe);
            AddRow("Giá vé:", $"{ve.GiaVe:N0} VND", underline: false);

            document.Add(table);
            document.Add(new Paragraph("\n"));
        }

        private void AddServiceInfo(Document document, List<ChiTietDichVu> services)
        {
            var titleFont = F(10, Font.BOLD);
            var contentFont = F(9);
            var headerFont = F(9, Font.BOLD);

            document.Add(new Paragraph("DỊCH VỤ ĐI KÈM", titleFont));

            var table = new PdfPTable(4) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 35, 15, 25, 25 });

            table.AddCell(new PdfPCell(new Phrase("Dịch vụ", headerFont)) { BackgroundColor = new BaseColor(230, 230, 230), Padding = 5, BorderColor = BaseColor.BLACK });
            table.AddCell(new PdfPCell(new Phrase("SL", headerFont)) { BackgroundColor = new BaseColor(230, 230, 230), Padding = 5, BorderColor = BaseColor.BLACK });
            table.AddCell(new PdfPCell(new Phrase("Đơn giá", headerFont)) { BackgroundColor = new BaseColor(230, 230, 230), Padding = 5, BorderColor = BaseColor.BLACK });
            table.AddCell(new PdfPCell(new Phrase("Thành tiền", headerFont)) { BackgroundColor = new BaseColor(230, 230, 230), Padding = 5, BorderColor = BaseColor.BLACK });

            foreach (var service in services)
            {
                table.AddCell(new PdfPCell(new Phrase(service.MaDichVuNavigation?.TenDichVu ?? "N/A", contentFont)) { BorderWidth = 0.8f, Padding = 5, BorderColor = new BaseColor(120, 120, 120) });
                table.AddCell(new PdfPCell(new Phrase(service.SoLuong.ToString(), contentFont)) { BorderWidth = 0.8f, Padding = 5, BorderColor = new BaseColor(120, 120, 120) });
                table.AddCell(new PdfPCell(new Phrase($"{service.DonGia:N0}", contentFont)) { BorderWidth = 0.8f, Padding = 5, BorderColor = new BaseColor(120, 120, 120) });
                table.AddCell(new PdfPCell(new Phrase($"{service.ThanhTien:N0}", contentFont)) { BorderWidth = 0.8f, Padding = 5, BorderColor = new BaseColor(120, 120, 120) });
            }

            document.Add(table);
            document.Add(new Paragraph("\n"));
        }

        private void AddPriceInfo(Document document, ChiTietVe ve, List<ChiTietDichVu> services)
        {
            var titleFont = F(10, Font.BOLD);
            var boldFont = F(10, Font.BOLD);
            var totalFont = F(12, Font.BOLD);

            document.Add(new Paragraph("TỔNG TIỀN", titleFont));

            var table = new PdfPTable(2) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 60, 40 });

            var cell = new PdfPCell { BorderWidth = 0, Padding = 6 };

            cell.Phrase = new Phrase("Vé:", boldFont); table.AddCell(cell);
            cell.Phrase = new Phrase($"{ve.GiaVe:N0} VND", boldFont); cell.HorizontalAlignment = Element.ALIGN_RIGHT; table.AddCell(cell);
            cell.HorizontalAlignment = Element.ALIGN_LEFT;

            var totalServices = services.Sum(s => s.ThanhTien);
            if (totalServices > 0)
            {
                cell.Phrase = new Phrase("Dịch vụ:", boldFont); table.AddCell(cell);
                cell.Phrase = new Phrase($"{totalServices:N0} VND", boldFont); cell.HorizontalAlignment = Element.ALIGN_RIGHT; table.AddCell(cell);
                cell.HorizontalAlignment = Element.ALIGN_LEFT;
            }

            cell.Border = Rectangle.BOTTOM_BORDER;
            cell.BorderColor = BaseColor.BLACK;
            cell.Phrase = new Phrase(string.Empty, boldFont);
            table.AddCell(cell);
            table.AddCell(cell);
            cell.Border = Rectangle.NO_BORDER;

            var totalAmount = ve.GiaVe + totalServices;
            cell.Phrase = new Phrase("TỔNG CỘNG:", totalFont); table.AddCell(cell);
            cell.Phrase = new Phrase($"{totalAmount:N0} VND", totalFont); cell.HorizontalAlignment = Element.ALIGN_RIGHT; table.AddCell(cell);
            cell.HorizontalAlignment = Element.ALIGN_LEFT;

            document.Add(table);
            document.Add(new Paragraph("\n"));
        }

        private void AddCustomerInfo(Document document, ChiTietVe ve)
        {
            var titleFont = F(10, Font.BOLD);
            var contentFont = F(9);
            var labelFont = F(9, Font.BOLD);

            document.Add(new Paragraph("THÔNG TIN KHÁCH HÀNG", titleFont));

            var table = new PdfPTable(2) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 40, 60 });

            var khachHang = ve.MaDonHangNavigation?.MaKhachHangNavigation;

            table.AddCell(new PdfPCell(new Phrase("Tên:", labelFont)) { BorderWidth = 0, Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(khachHang?.HoTen ?? "N/A", contentFont)) { BorderWidth = 0, Padding = 5 });

            table.AddCell(new PdfPCell(new Phrase("Email:", labelFont)) { BorderWidth = 0, Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(khachHang?.Email ?? "N/A", contentFont)) { BorderWidth = 0, Padding = 5 });

            table.AddCell(new PdfPCell(new Phrase("SĐT:", labelFont)) { BorderWidth = 0, Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(khachHang?.SoDienThoai ?? "N/A", contentFont)) { BorderWidth = 0, Padding = 5 });

            table.AddCell(new PdfPCell(new Phrase("Mã đơn:", labelFont)) { BorderWidth = 0, Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(ve.MaDonHang, contentFont)) { BorderWidth = 0, Padding = 5 });

            document.Add(table);
            document.Add(new Paragraph("\n"));
        }

        private void AddRules(Document document)
        {
            var titleFont = F(10, Font.BOLD);
            var contentFont = F(8);

            document.Add(new Paragraph("QUY ĐỊNH", titleFont));

            var rules = new List<string>
            {
                "- Vào rạp trước giờ chiếu 15 phút.",
                "- Xuất trình vé/PDF khi check-in.",
                "- Vé chỉ có giá trị cho đúng suất chiếu.",
                "- Không hoàn/đổi sau khi thanh toán."
            };

            foreach (var rule in rules)
            {
                document.Add(new Paragraph(rule, contentFont) { SpacingAfter = 2f });
            }

            document.Add(new Paragraph("\n"));
        }

        private void AddFooter(Document document)
        {
            var divider = new PdfPTable(1) { WidthPercentage = 100 };
            divider.AddCell(new PdfPCell(new Phrase(""))
            {
                BorderWidth = 0,
                Border = Rectangle.BOTTOM_BORDER,
                BorderColor = new BaseColor(120, 120, 120)
            });
            document.Add(divider);

            var footerFont = F(8);
            var footerText = $"Cảm ơn quý khách!\nNgày in: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            var footer = new Paragraph(footerText, footerFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingBefore = 5f
            };
            document.Add(footer);
        }
    }
}