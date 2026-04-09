-- ============================================================
--  HỆ THỐNG QUẢN LÝ RẠP CHIẾU PHIM
--  Schema.sql — Tạo database + bảng + index
--  Nhóm 1 | ASP.NET MVC + EF Core | .NET 8
--  Lưu ý: Toàn bộ business logic xử lý trong Model/Service C#
--  Thứ tự tạo bảng: Tầng 1 → 5 (theo dependency)
--
--  Cập nhật:
--  + Phim       : thêm DuongDanTrailer (NVARCHAR 500)
--  + DichVu     : thêm DuongDanHinh    (NVARCHAR 500)
-- ============================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'RapChieuPhimDB')
    DROP DATABASE RapChieuPhimDB;
GO

CREATE DATABASE RapChieuPhimDB
    COLLATE Vietnamese_CI_AS;
GO

USE RapChieuPhimDB;
GO

-- ============================================================
-- TẦNG 1 — Bảng độc lập (không có FK)
-- ============================================================

CREATE TABLE TheLoaiPhim (
    MaTheLoai   NVARCHAR(10)  NOT NULL PRIMARY KEY,
    TenTheLoai  NVARCHAR(100) NOT NULL,
    DaXoa       BIT           NOT NULL DEFAULT 0
);

CREATE TABLE LoaiGhe (
    MaLoaiGhe   NVARCHAR(10)  NOT NULL PRIMARY KEY,
    TenLoaiGhe  NVARCHAR(50)  NOT NULL,
    HeSoGia     FLOAT         NOT NULL DEFAULT 1.0,
    DaXoa       BIT           NOT NULL DEFAULT 0
);

CREATE TABLE LoaiPhong (
    MaLoaiPhong   NVARCHAR(10)  NOT NULL PRIMARY KEY,
    TenLoaiPhong  NVARCHAR(50)  NOT NULL,
    DaXoa         BIT           NOT NULL DEFAULT 0
);

CREATE TABLE LoaiKhachHang (
    MaLoaiKH        NVARCHAR(10)  NOT NULL PRIMARY KEY,
    TenLoaiKH       NVARCHAR(50)  NOT NULL,
    NguongDiem      INT           NOT NULL DEFAULT 0,
    PhanTramGiamGia FLOAT         NOT NULL DEFAULT 0,
    DaXoa           BIT           NOT NULL DEFAULT 0
);

CREATE TABLE DanhMucDichVu (
    MaDanhMuc   NVARCHAR(10)  NOT NULL PRIMARY KEY,
    TenDanhMuc  NVARCHAR(100) NOT NULL,
    DaXoa       BIT           NOT NULL DEFAULT 0
);

-- ============================================================
-- TẦNG 2 — Phụ thuộc Tầng 1
-- ============================================================

CREATE TABLE Phim (
    MaPhim           NVARCHAR(10)  NOT NULL PRIMARY KEY,
    TenPhim          NVARCHAR(200) NOT NULL,
    MaTheLoai        NVARCHAR(10)  NOT NULL,
    ThoiLuong        INT           NOT NULL,
    NgayPhatHanh     DATE          NOT NULL,
    MoTa             NVARCHAR(MAX) NULL,
    PhanLoaiDoTuoi   NVARCHAR(10)  NULL,        -- P | K | T13 | T16 | T18
    DuongDanAnh      NVARCHAR(500) NULL,        -- URL poster/thumbnail
    DuongDanTrailer  NVARCHAR(500) NULL,        -- URL YouTube embed hoặc video
    TrangThai        NVARCHAR(20)  NOT NULL DEFAULT N'DangChieu',
                                                -- DangChieu | SapChieu | NgungChieu
    DaXoa            BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_Phim_TheLoai FOREIGN KEY (MaTheLoai)
        REFERENCES TheLoaiPhim(MaTheLoai)
);

CREATE TABLE PhongChieu (
    MaPhong     NVARCHAR(10) NOT NULL PRIMARY KEY,
    TenPhong    NVARCHAR(50) NOT NULL,
    MaLoaiPhong NVARCHAR(10) NOT NULL,
    SoGhe       INT          NOT NULL DEFAULT 0,
    TrangThai   NVARCHAR(20) NOT NULL DEFAULT N'HoatDong',
                                        -- HoatDong | BaoDuong | Dong
    DaXoa       BIT          NOT NULL DEFAULT 0,
    CONSTRAINT FK_PhongChieu_LoaiPhong FOREIGN KEY (MaLoaiPhong)
        REFERENCES LoaiPhong(MaLoaiPhong)
);

CREATE TABLE KhachHang (
    MaKhachHang     NVARCHAR(10)  NOT NULL PRIMARY KEY,
    HoTen           NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(150) NOT NULL UNIQUE,
    SoDienThoai     NVARCHAR(15)  NULL,
    NgaySinh        DATE          NULL,
    GioiTinh        NVARCHAR(10)  NULL,
    MaLoaiKH        NVARCHAR(10)  NOT NULL DEFAULT 'LKH01',
    DiemTichLuy     INT           NOT NULL DEFAULT 0,
    HangThanhVien   NVARCHAR(50)  NULL,
    PhanTramGiamGia FLOAT         NOT NULL DEFAULT 0,
    DaXoa           BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_KhachHang_LoaiKH FOREIGN KEY (MaLoaiKH)
        REFERENCES LoaiKhachHang(MaLoaiKH)
);

CREATE TABLE NhanVien (
    MaNhanVien  NVARCHAR(10)  NOT NULL PRIMARY KEY,
    HoTen       NVARCHAR(100) NOT NULL,
    NgaySinh    DATE          NULL,
    GioiTinh    NVARCHAR(10)  NULL,
    DiaChi      NVARCHAR(200) NULL,
    NgayVaoLam  DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    CaLamViec   NVARCHAR(50)  NULL,
    DaXoa       BIT           NOT NULL DEFAULT 0
);

CREATE TABLE TaiKhoan (
    TenDangNhap NVARCHAR(50)  NOT NULL PRIMARY KEY,
    MatKhau     NVARCHAR(256) NOT NULL,          -- BCrypt hash
    VaiTro      NVARCHAR(20)  NOT NULL DEFAULT N'KhachHang',
                                                 -- KhachHang | NhanVien | Admin
    TrangThai   NVARCHAR(20)  NOT NULL DEFAULT N'HoatDong',
                                                 -- HoatDong | BiKhoa
    MaKhachHang NVARCHAR(10)  NULL,
    MaNhanVien  NVARCHAR(10)  NULL,
    DaXoa       BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_TaiKhoan_KhachHang FOREIGN KEY (MaKhachHang)
        REFERENCES KhachHang(MaKhachHang),
    CONSTRAINT FK_TaiKhoan_NhanVien  FOREIGN KEY (MaNhanVien)
        REFERENCES NhanVien(MaNhanVien)
);

CREATE TABLE DichVu (
    MaDichVu      NVARCHAR(10)  NOT NULL PRIMARY KEY,
    TenDichVu     NVARCHAR(100) NOT NULL,
    MaDanhMuc     NVARCHAR(10)  NOT NULL,
    GiaBan        FLOAT         NOT NULL,
    SoLuongTon    INT           NOT NULL DEFAULT 0,
    DuongDanHinh  NVARCHAR(500) NULL,            -- URL hình ảnh sản phẩm
    DaXoa         BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_DichVu_DanhMuc FOREIGN KEY (MaDanhMuc)
        REFERENCES DanhMucDichVu(MaDanhMuc)
);

-- ============================================================
-- TẦNG 3 — Phụ thuộc Tầng 2
-- ============================================================

CREATE TABLE Ghe (
    MaGhe     NVARCHAR(10) NOT NULL PRIMARY KEY,
    MaPhong   NVARCHAR(10) NOT NULL,
    TenHang   NVARCHAR(5)  NOT NULL,     -- A, B, C...
    SoThu     NVARCHAR(5)  NOT NULL,     -- 1, 2, 3...
    MaLoaiGhe NVARCHAR(10) NOT NULL,
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'Trong',
                                         -- Trong | DangGiu | DaDat | BaoTri
    DaXoa     BIT          NOT NULL DEFAULT 0,
    CONSTRAINT FK_Ghe_PhongChieu FOREIGN KEY (MaPhong)
        REFERENCES PhongChieu(MaPhong),
    CONSTRAINT FK_Ghe_LoaiGhe FOREIGN KEY (MaLoaiGhe)
        REFERENCES LoaiGhe(MaLoaiGhe)
);

CREATE TABLE SuatChieu (
    MaSuatChieu     NVARCHAR(10) NOT NULL PRIMARY KEY,
    MaPhim          NVARCHAR(10) NOT NULL,
    MaPhong         NVARCHAR(10) NOT NULL,
    ThoiGianBatDau  DATETIME     NOT NULL,
    ThoiGianKetThuc DATETIME     NOT NULL,
    GiaGoc          FLOAT        NOT NULL,
    TrangThai       NVARCHAR(20) NOT NULL DEFAULT N'DaLenLich',
                                         -- DaLenLich | DangChieu | DaKetThuc | DaHuy
    DaXoa           BIT          NOT NULL DEFAULT 0,
    CONSTRAINT FK_SuatChieu_Phim       FOREIGN KEY (MaPhim)
        REFERENCES Phim(MaPhim),
    CONSTRAINT FK_SuatChieu_PhongChieu FOREIGN KEY (MaPhong)
        REFERENCES PhongChieu(MaPhong)
);

CREATE TABLE KhuyenMai (
    MaKhuyenMai   NVARCHAR(10) NOT NULL PRIMARY KEY,
    MaCode        NVARCHAR(50) NOT NULL UNIQUE,
    PhanTramGiam  FLOAT        NOT NULL,
    GiamToiDa     FLOAT        NULL,
    TuNgay        DATETIME     NOT NULL,
    DenNgay       DATETIME     NOT NULL,
    SoLuongConLai INT          NOT NULL DEFAULT 0,
    DonToiThieu   FLOAT        NOT NULL DEFAULT 0,
    TrangThai     NVARCHAR(20) NOT NULL DEFAULT N'ChoKichHoat',
                                         -- ChoKichHoat | DangApDung | DaKetThuc
    DaXoa         BIT          NOT NULL DEFAULT 0
);

-- ============================================================
-- TẦNG 4 — Nghiệp vụ chính
-- ============================================================

CREATE TABLE DonHang (
    MaDonHang       NVARCHAR(15) NOT NULL PRIMARY KEY,
    MaKhachHang     NVARCHAR(10) NULL,   -- NULL nếu khách vãng lai tại quầy
    MaNhanVien      NVARCHAR(10) NULL,   -- NULL nếu đặt online
    MaKhuyenMai     NVARCHAR(10) NULL,
    NgayTao         DATETIME     NOT NULL DEFAULT GETDATE(),
    TongTienBanDau  FLOAT        NOT NULL DEFAULT 0,
    TongTienSauGiam FLOAT        NOT NULL DEFAULT 0,
    TrangThai       NVARCHAR(20) NOT NULL DEFAULT N'ChoThanhToan',
                                         -- ChoThanhToan | DaThanhToan | DaHuy
    DaXoa           BIT          NOT NULL DEFAULT 0,
    CONSTRAINT FK_DonHang_KhachHang FOREIGN KEY (MaKhachHang)
        REFERENCES KhachHang(MaKhachHang),
    CONSTRAINT FK_DonHang_NhanVien  FOREIGN KEY (MaNhanVien)
        REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_DonHang_KhuyenMai FOREIGN KEY (MaKhuyenMai)
        REFERENCES KhuyenMai(MaKhuyenMai)
);

CREATE TABLE ThanhToan (
    MaThanhToan     NVARCHAR(15)  NOT NULL PRIMARY KEY,
    MaDonHang       NVARCHAR(15)  NOT NULL,
    PhuongThuc      NVARCHAR(30)  NOT NULL, -- TienMat | VNPay | Momo | ZaloPay
    SoTien          FLOAT         NOT NULL,
    NgayThanhToan   DATETIME      NOT NULL DEFAULT GETDATE(),
    TrangThai       NVARCHAR(20)  NOT NULL DEFAULT N'ChoXuLy',
                                            -- ChoXuLy | ThanhCong | ThatBai
    MaGiaoDichNgoai NVARCHAR(100) NULL,     -- TransactionID từ cổng thanh toán
    DaXoa           BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_ThanhToan_DonHang FOREIGN KEY (MaDonHang)
        REFERENCES DonHang(MaDonHang)
);

-- ============================================================
-- TẦNG 5 — Chi tiết giao dịch
-- ============================================================

CREATE TABLE ChiTietVe (
    MaVe        NVARCHAR(15)  NOT NULL PRIMARY KEY,
    MaDonHang   NVARCHAR(15)  NOT NULL,
    MaSuatChieu NVARCHAR(10)  NOT NULL,
    MaGhe       NVARCHAR(10)  NOT NULL,
    GiaVe       FLOAT         NOT NULL,
    MaQR        NVARCHAR(200) NULL,      -- Guid unique, sinh trong TicketService C#
    TrangThai   NVARCHAR(20)  NOT NULL DEFAULT N'ChuaSuDung',
                                         -- ChuaSuDung | DaSuDung | DaHuy | HetHan
    DaXoa       BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_ChiTietVe_DonHang     FOREIGN KEY (MaDonHang)
        REFERENCES DonHang(MaDonHang),
    CONSTRAINT FK_ChiTietVe_SuatChieu   FOREIGN KEY (MaSuatChieu)
        REFERENCES SuatChieu(MaSuatChieu),
    CONSTRAINT FK_ChiTietVe_Ghe         FOREIGN KEY (MaGhe)
        REFERENCES Ghe(MaGhe)
);

CREATE TABLE ChiTietDichVu (
    MaChiTiet NVARCHAR(15) NOT NULL PRIMARY KEY,
    MaDonHang NVARCHAR(15) NOT NULL,
    MaDichVu  NVARCHAR(10) NOT NULL,
    SoLuong   INT          NOT NULL DEFAULT 1,
    DonGia    FLOAT        NOT NULL,
    ThanhTien AS (SoLuong * DonGia) PERSISTED,
    DaXoa     BIT          NOT NULL DEFAULT 0,
    CONSTRAINT FK_ChiTietDichVu_DonHang FOREIGN KEY (MaDonHang)
        REFERENCES DonHang(MaDonHang),
    CONSTRAINT FK_ChiTietDichVu_DichVu  FOREIGN KEY (MaDichVu)
        REFERENCES DichVu(MaDichVu)
);

-- ============================================================
-- INDEXES
-- ============================================================

CREATE INDEX IX_SuatChieu_MaPhim           ON SuatChieu(MaPhim);
CREATE INDEX IX_SuatChieu_MaPhong_ThoiGian ON SuatChieu(MaPhong, ThoiGianBatDau);
CREATE INDEX IX_Ghe_MaPhong_TrangThai      ON Ghe(MaPhong, TrangThai);
CREATE UNIQUE INDEX IX_ChiTietVe_MaQR      ON ChiTietVe(MaQR) WHERE MaQR IS NOT NULL;
CREATE INDEX IX_DonHang_MaKhachHang        ON DonHang(MaKhachHang);
CREATE INDEX IX_KhuyenMai_MaCode           ON KhuyenMai(MaCode);
CREATE INDEX IX_TaiKhoan_MaKhachHang       ON TaiKhoan(MaKhachHang);

PRINT N'';
PRINT N'✅ Schema tạo thành công!';
PRINT N'✅ 15 bảng + 7 Index sẵn sàng';
PRINT N'✅ Chạy tiếp Data.sql để chèn dữ liệu mẫu';