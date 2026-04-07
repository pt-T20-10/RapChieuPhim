using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RapChieuPhim.Models.Entities;

namespace RapChieuPhim.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietDichVu> ChiTietDichVus { get; set; }

    public virtual DbSet<ChiTietVe> ChiTietVes { get; set; }

    public virtual DbSet<DanhMucDichVu> DanhMucDichVus { get; set; }

    public virtual DbSet<DichVu> DichVus { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<Ghe> Ghes { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<KhuyenMai> KhuyenMais { get; set; }

    public virtual DbSet<LoaiGhe> LoaiGhes { get; set; }

    public virtual DbSet<LoaiKhachHang> LoaiKhachHangs { get; set; }

    public virtual DbSet<LoaiPhong> LoaiPhongs { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<Phim> Phims { get; set; }

    public virtual DbSet<PhongChieu> PhongChieus { get; set; }

    public virtual DbSet<SuatChieu> SuatChieus { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<TheLoaiPhim> TheLoaiPhims { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<ChiTietDichVu>(entity =>
        {
            entity.HasKey(e => e.MaChiTiet).HasName("PK__ChiTietD__CDF0A1144B03C422");

            entity.ToTable("ChiTietDichVu");

            entity.Property(e => e.MaChiTiet).HasMaxLength(15);
            entity.Property(e => e.MaDichVu).HasMaxLength(10);
            entity.Property(e => e.MaDonHang).HasMaxLength(15);
            entity.Property(e => e.SoLuong).HasDefaultValue(1);
            entity.Property(e => e.ThanhTien).HasComputedColumnSql("([SoLuong]*[DonGia])", true);

            entity.HasOne(d => d.MaDichVuNavigation).WithMany(p => p.ChiTietDichVus)
                .HasForeignKey(d => d.MaDichVu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietDichVu_DichVu");

            entity.HasOne(d => d.MaDonHangNavigation).WithMany(p => p.ChiTietDichVus)
                .HasForeignKey(d => d.MaDonHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietDichVu_DonHang");
        });

        modelBuilder.Entity<ChiTietVe>(entity =>
        {
            entity.HasKey(e => e.MaVe).HasName("PK__ChiTietV__2725100F7BD8C034");

            entity.ToTable("ChiTietVe");

            entity.HasIndex(e => e.MaQr, "IX_ChiTietVe_MaQR")
                .IsUnique()
                .HasFilter("([MaQR] IS NOT NULL)");

            entity.Property(e => e.MaVe).HasMaxLength(15);
            entity.Property(e => e.MaDonHang).HasMaxLength(15);
            entity.Property(e => e.MaGhe).HasMaxLength(10);
            entity.Property(e => e.MaQr)
                .HasMaxLength(200)
                .HasColumnName("MaQR");
            entity.Property(e => e.MaSuatChieu).HasMaxLength(10);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("ChuaSuDung");

            entity.HasOne(d => d.MaDonHangNavigation).WithMany(p => p.ChiTietVes)
                .HasForeignKey(d => d.MaDonHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietVe_DonHang");

            entity.HasOne(d => d.MaGheNavigation).WithMany(p => p.ChiTietVes)
                .HasForeignKey(d => d.MaGhe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietVe_Ghe");

            entity.HasOne(d => d.MaSuatChieuNavigation).WithMany(p => p.ChiTietVes)
                .HasForeignKey(d => d.MaSuatChieu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietVe_SuatChieu");
        });

        modelBuilder.Entity<DanhMucDichVu>(entity =>
        {
            entity.HasKey(e => e.MaDanhMuc).HasName("PK__DanhMucD__B375088788EFE9F6");

            entity.ToTable("DanhMucDichVu");

            entity.Property(e => e.MaDanhMuc).HasMaxLength(10);
            entity.Property(e => e.TenDanhMuc).HasMaxLength(100);
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.HasKey(e => e.MaDichVu).HasName("PK__DichVu__C0E6DE8F0190AF83");

            entity.ToTable("DichVu");

            entity.Property(e => e.MaDichVu).HasMaxLength(10);
            entity.Property(e => e.MaDanhMuc).HasMaxLength(10);
            entity.Property(e => e.TenDichVu).HasMaxLength(100);

            entity.HasOne(d => d.MaDanhMucNavigation).WithMany(p => p.DichVus)
                .HasForeignKey(d => d.MaDanhMuc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DichVu_DanhMuc");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.MaDonHang).HasName("PK__DonHang__129584AD1D9B0A6A");

            entity.ToTable("DonHang");

            entity.HasIndex(e => e.MaKhachHang, "IX_DonHang_MaKhachHang");

            entity.Property(e => e.MaDonHang).HasMaxLength(15);
            entity.Property(e => e.MaKhachHang).HasMaxLength(10);
            entity.Property(e => e.MaKhuyenMai).HasMaxLength(10);
            entity.Property(e => e.MaNhanVien).HasMaxLength(10);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("ChoThanhToan");

            entity.HasOne(d => d.MaKhachHangNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.MaKhachHang)
                .HasConstraintName("FK_DonHang_KhachHang");

            entity.HasOne(d => d.MaKhuyenMaiNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.MaKhuyenMai)
                .HasConstraintName("FK_DonHang_KhuyenMai");

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.MaNhanVien)
                .HasConstraintName("FK_DonHang_NhanVien");
        });

        modelBuilder.Entity<Ghe>(entity =>
        {
            entity.HasKey(e => e.MaGhe).HasName("PK__Ghe__3CD3C67B46C31957");

            entity.ToTable("Ghe");

            entity.HasIndex(e => new { e.MaPhong, e.TrangThai }, "IX_Ghe_MaPhong_TrangThai");

            entity.Property(e => e.MaGhe).HasMaxLength(10);
            entity.Property(e => e.MaLoaiGhe).HasMaxLength(10);
            entity.Property(e => e.MaPhong).HasMaxLength(10);
            entity.Property(e => e.SoThu).HasMaxLength(5);
            entity.Property(e => e.TenHang).HasMaxLength(5);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Trong");

            entity.HasOne(d => d.MaLoaiGheNavigation).WithMany(p => p.Ghes)
                .HasForeignKey(d => d.MaLoaiGhe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ghe_LoaiGhe");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.Ghes)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ghe_PhongChieu");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.MaKhachHang).HasName("PK__KhachHan__88D2F0E595B7E847");

            entity.ToTable("KhachHang");

            entity.HasIndex(e => e.Email, "UQ__KhachHan__A9D1053414E68D93").IsUnique();

            entity.Property(e => e.MaKhachHang).HasMaxLength(10);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HangThanhVien).HasMaxLength(50);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MaLoaiKh)
                .HasMaxLength(10)
                .HasDefaultValue("LKH01")
                .HasColumnName("MaLoaiKH");
            entity.Property(e => e.SoDienThoai).HasMaxLength(15);

            entity.HasOne(d => d.MaLoaiKhNavigation).WithMany(p => p.KhachHangs)
                .HasForeignKey(d => d.MaLoaiKh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KhachHang_LoaiKH");
        });

        modelBuilder.Entity<KhuyenMai>(entity =>
        {
            entity.HasKey(e => e.MaKhuyenMai).HasName("PK__KhuyenMa__6F56B3BD705A28A0");

            entity.ToTable("KhuyenMai");

            entity.HasIndex(e => e.MaCode, "IX_KhuyenMai_MaCode");

            entity.HasIndex(e => e.MaCode, "UQ__KhuyenMa__152C7C5C6EFEF6D8").IsUnique();

            entity.Property(e => e.MaKhuyenMai).HasMaxLength(10);
            entity.Property(e => e.DenNgay).HasColumnType("datetime");
            entity.Property(e => e.MaCode).HasMaxLength(50);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("ChoKichHoat");
            entity.Property(e => e.TuNgay).HasColumnType("datetime");
        });

        modelBuilder.Entity<LoaiGhe>(entity =>
        {
            entity.HasKey(e => e.MaLoaiGhe).HasName("PK__LoaiGhe__965BB4C133D8763D");

            entity.ToTable("LoaiGhe");

            entity.Property(e => e.MaLoaiGhe).HasMaxLength(10);
            entity.Property(e => e.HeSoGia).HasDefaultValue(1.0);
            entity.Property(e => e.TenLoaiGhe).HasMaxLength(50);
        });

        modelBuilder.Entity<LoaiKhachHang>(entity =>
        {
            entity.HasKey(e => e.MaLoaiKh).HasName("PK__LoaiKhac__12250B7E132ADB07");

            entity.ToTable("LoaiKhachHang");

            entity.Property(e => e.MaLoaiKh)
                .HasMaxLength(10)
                .HasColumnName("MaLoaiKH");
            entity.Property(e => e.TenLoaiKh)
                .HasMaxLength(50)
                .HasColumnName("TenLoaiKH");
        });

        modelBuilder.Entity<LoaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaLoaiPhong).HasName("PK__LoaiPhon__2302121788EE506F");

            entity.ToTable("LoaiPhong");

            entity.Property(e => e.MaLoaiPhong).HasMaxLength(10);
            entity.Property(e => e.TenLoaiPhong).HasMaxLength(50);
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNhanVien).HasName("PK__NhanVien__77B2CA477B7D9E0F");

            entity.ToTable("NhanVien");

            entity.Property(e => e.MaNhanVien).HasMaxLength(10);
            entity.Property(e => e.CaLamViec).HasMaxLength(50);
            entity.Property(e => e.DiaChi).HasMaxLength(200);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.NgayVaoLam).HasDefaultValueSql("(CONVERT([date],getdate()))");
        });

        modelBuilder.Entity<Phim>(entity =>
        {
            entity.HasKey(e => e.MaPhim).HasName("PK__Phim__4AC03DE365EA46EA");

            entity.ToTable("Phim");

            entity.Property(e => e.MaPhim).HasMaxLength(10);
            entity.Property(e => e.DuongDanAnh).HasMaxLength(500);
            entity.Property(e => e.MaTheLoai).HasMaxLength(10);
            entity.Property(e => e.PhanLoaiDoTuoi).HasMaxLength(10);
            entity.Property(e => e.TenPhim).HasMaxLength(200);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("DangChieu");

            entity.HasOne(d => d.MaTheLoaiNavigation).WithMany(p => p.Phims)
                .HasForeignKey(d => d.MaTheLoai)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phim_TheLoai");
        });

        modelBuilder.Entity<PhongChieu>(entity =>
        {
            entity.HasKey(e => e.MaPhong).HasName("PK__PhongChi__20BD5E5B79186F29");

            entity.ToTable("PhongChieu");

            entity.Property(e => e.MaPhong).HasMaxLength(10);
            entity.Property(e => e.MaLoaiPhong).HasMaxLength(10);
            entity.Property(e => e.TenPhong).HasMaxLength(50);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("HoatDong");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.PhongChieus)
                .HasForeignKey(d => d.MaLoaiPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhongChieu_LoaiPhong");
        });

        modelBuilder.Entity<SuatChieu>(entity =>
        {
            entity.HasKey(e => e.MaSuatChieu).HasName("PK__SuatChie__CF5984D2A262B62C");

            entity.ToTable("SuatChieu");

            entity.HasIndex(e => e.MaPhim, "IX_SuatChieu_MaPhim");

            entity.HasIndex(e => new { e.MaPhong, e.ThoiGianBatDau }, "IX_SuatChieu_MaPhong_ThoiGian");

            entity.Property(e => e.MaSuatChieu).HasMaxLength(10);
            entity.Property(e => e.MaPhim).HasMaxLength(10);
            entity.Property(e => e.MaPhong).HasMaxLength(10);
            entity.Property(e => e.ThoiGianBatDau).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianKetThuc).HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("DaLenLich");

            entity.HasOne(d => d.MaPhimNavigation).WithMany(p => p.SuatChieus)
                .HasForeignKey(d => d.MaPhim)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SuatChieu_Phim");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.SuatChieus)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SuatChieu_PhongChieu");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.TenDangNhap).HasName("PK__TaiKhoan__55F68FC196FBEF7B");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.MaKhachHang, "IX_TaiKhoan_MaKhachHang");

            entity.Property(e => e.TenDangNhap).HasMaxLength(50);
            entity.Property(e => e.MaKhachHang).HasMaxLength(10);
            entity.Property(e => e.MaNhanVien).HasMaxLength(10);
            entity.Property(e => e.MatKhau).HasMaxLength(256);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("HoatDong");
            entity.Property(e => e.VaiTro)
                .HasMaxLength(20)
                .HasDefaultValue("KhachHang");

            entity.HasOne(d => d.MaKhachHangNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.MaKhachHang)
                .HasConstraintName("FK_TaiKhoan_KhachHang");

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.MaNhanVien)
                .HasConstraintName("FK_TaiKhoan_NhanVien");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.MaThanhToan).HasName("PK__ThanhToa__D4B25844CA19966F");

            entity.ToTable("ThanhToan");

            entity.Property(e => e.MaThanhToan).HasMaxLength(15);
            entity.Property(e => e.MaDonHang).HasMaxLength(15);
            entity.Property(e => e.MaGiaoDichNgoai).HasMaxLength(100);
            entity.Property(e => e.NgayThanhToan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhuongThuc).HasMaxLength(30);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("ChoXuLy");

            entity.HasOne(d => d.MaDonHangNavigation).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.MaDonHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThanhToan_DonHang");
        });

        modelBuilder.Entity<TheLoaiPhim>(entity =>
        {
            entity.HasKey(e => e.MaTheLoai).HasName("PK__TheLoaiP__D73FF34A4D2D9A1F");

            entity.ToTable("TheLoaiPhim");

            entity.Property(e => e.MaTheLoai).HasMaxLength(10);
            entity.Property(e => e.TenTheLoai).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
