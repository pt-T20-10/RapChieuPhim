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

    // ✅ API để lấy QR code của vé (dùng để test quét)
    public async Task<IActionResult> OnGetAsync(string maVe)
    {
        if (string.IsNullOrWhiteSpace(maVe))
        {
            return BadRequest(new { message = "MaVe không hợp lệ" });
        }

        // Lấy vé từ DB
        var ve = await _dbContext.ChiTietVe
            .FirstOrDefaultAsync(v => v.MaVe == maVe);

        if (ve == null)
        {
            return NotFound(new { message = "Vé không tồn tại" });
        }

        if (string.IsNullOrWhiteSpace(ve.MaQr))
        {
            ve.MaQr = maVe;
        }

        // Generate QR code
        string qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(ve.MaQr);

        return new JsonResult(new
        {
            maVe = ve.MaVe,
            maQr = ve.MaQr,
            qrCodeBase64 = qrCodeBase64,
            message = "QR code được tạo thành công"
        });
    }

    // ✅ API để test QR code - tạo QR từ text bất kỳ
    public IActionResult OnGetTestQr(string text = "TEST_QR_001")
    {
        try
        {
            string qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(text);

            return new JsonResult(new
            {
                text = text,
                qrCodeBase64 = qrCodeBase64,
                message = "✅ Test QR code được tạo - dùng để quét test"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}