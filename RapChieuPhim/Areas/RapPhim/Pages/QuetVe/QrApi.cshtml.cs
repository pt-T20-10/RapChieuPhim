using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RapChieuPhim.Data;
using RapChieuPhim.Services;
using Microsoft.EntityFrameworkCore;

namespace RapChieuPhim.Areas.RapPhim.Pages.QuetVe;

[IgnoreAntiforgeryToken]
public class QrApiModel : PageModel
{
    private readonly QRCodeService _qrCodeService;
    private readonly AppDbContext _dbContext;

    public QrApiModel(QRCodeService qrCodeService, AppDbContext dbContext)
    {
        _qrCodeService = qrCodeService;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGetAsync(string maVe)
    {
        if (string.IsNullOrWhiteSpace(maVe))
        {
            return BadRequest(new { message = "MaVe không hợp lệ" });
        }

        // Lấy vé từ DB
        var ve = await _dbContext.ChiTietVes
            .FirstOrDefaultAsync(v => v.MaVe == maVe);

        if (ve == null)
        {
            return NotFound(new { message = "Vé không tồn tại" });
        }

        if (string.IsNullOrWhiteSpace(ve.MaQr))
        {
            // Nếu chưa có QR, tạo mới từ MaVe
            ve.MaQr = maVe;
        }

        // Generate QR code
        string qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(ve.MaQr);

        return new JsonResult(new
        {
            maVe = ve.MaVe,
            maQr = ve.MaQr,
            qrCodeImage = qrCodeBase64,
            trangThai = ve.TrangThai
        });
    }
}