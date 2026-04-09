using System;
using System.IO;
using QRCoder;

namespace RapChieuPhim.Services;

public class QRCodeService
{
    /// <summary>
    /// Tạo QR code dưới dạng Base64 string (để hiển thị trong HTML)
    /// </summary>
    public string GenerateQRCodeBase64(string data)
    {
        try
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                
                // ✅ Dùng PngByteQRCode thay vì QRCode
                var pngQrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeImage = pngQrCode.GetGraphic(10);
                
                string base64String = Convert.ToBase64String(qrCodeImage);
                return $"data:image/png;base64,{base64String}";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi tạo QR code: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tạo QR code dưới dạng file PNG bytes
    /// </summary>
    public byte[] GenerateQRCodeBytes(string data)
    {
        try
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                
                // ✅ Dùng PngByteQRCode
                var pngQrCode = new PngByteQRCode(qrCodeData);
                return pngQrCode.GetGraphic(10);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi tạo QR code: {ex.Message}", ex);
        }
    }
}