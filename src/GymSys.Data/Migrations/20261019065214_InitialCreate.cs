using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymSys.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    MemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KodeMember = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NamaLengkap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoTelepon = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Alamat = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TglLahir = table.Column<DateOnly>(type: "date", nullable: true),
                    JenisKelamin = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: true),
                    KontakDarurat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CatatanKesehatan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TglBergabung = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CAST(GETDATE() AS DATE)"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.MemberId);
                    table.CheckConstraint("CK_Members_JenisKelamin", "[JenisKelamin] IN ('L','P')");
                });

            migrationBuilder.CreateTable(
                name: "MembershipPlans",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaPaket = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurasiHari = table.Column<int>(type: "int", nullable: false),
                    Harga = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Deskripsi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPlans", x => x.PlanId);
                    table.CheckConstraint("CK_Plans_DurasiHari", "[DurasiHari] > 0");
                    table.CheckConstraint("CK_Plans_Harga", "[Harga] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(16)", maxLength: 16, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NamaLengkap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Theme = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Dark"),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN ('Admin','Resepsionis','Instruktur')");
                });

            migrationBuilder.CreateTable(
                name: "Trainers",
                columns: table => new
                {
                    TrainerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Nama = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoTelepon = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Keahlian = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trainers", x => x.TrainerId);
                    table.ForeignKey(
                        name: "FK_Trainers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NoKuitansi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    UserIdKasir = table.Column<int>(type: "int", nullable: false),
                    TglTransaksi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    TglMulaiAktif = table.Column<DateOnly>(type: "date", nullable: false),
                    TglBerakhir = table.Column<DateOnly>(type: "date", nullable: false),
                    HargaPaket = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Diskon = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    TotalBayar = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MetodeBayar = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Lunas"),
                    AlasanBatal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                    table.CheckConstraint("CK_Trx_Diskon", "[Diskon] <= [HargaPaket]");
                    table.CheckConstraint("CK_Trx_MetodeBayar", "[MetodeBayar] IN ('Tunai','Transfer')");
                    table.CheckConstraint("CK_Trx_Status", "[Status] IN ('Lunas','Batal')");
                    table.CheckConstraint("CK_Trx_Tanggal", "[TglBerakhir] >= [TglMulaiAktif]");
                    table.ForeignKey(
                        name: "FK_Transactions_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_MembershipPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "MembershipPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserIdKasir",
                        column: x => x.UserIdKasir,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PTAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    TrainerId = table.Column<int>(type: "int", nullable: false),
                    TglMulai = table.Column<DateOnly>(type: "date", nullable: false),
                    TglSelesai = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_PTAssignments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PTAssignments_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "TrainerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MembershipPlans",
                columns: new[] { "PlanId", "Deskripsi", "DurasiHari", "Harga", "IsActive", "NamaPaket" },
                values: new object[,]
                {
                    { 1, "Akses gym 30 hari", 30, 250000m, true, "Bronze 1 Bulan" },
                    { 2, "Akses gym 90 hari", 90, 675000m, true, "Silver 3 Bulan" },
                    { 3, "Akses gym 365 hari", 365, 2400000m, true, "Gold 12 Bulan" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "IsActive", "LockedUntil", "MustChangePassword", "NamaLengkap", "PasswordHash", "PasswordSalt", "Role", "Theme", "Username" },
                values: new object[] { 1, true, null, true, "Administrator", new byte[] { 32, 4, 180, 48, 244, 146, 11, 188, 27, 51, 80, 255, 157, 161, 221, 87, 196, 69, 112, 107, 226, 70, 134, 11, 122, 70, 67, 229, 117, 152, 68, 218 }, new byte[] { 71, 89, 77, 45, 83, 89, 83, 45, 97, 100, 109, 105, 110, 45, 118, 50 }, "Admin", "Dark", "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_Members_KodeMember",
                table: "Members",
                column: "KodeMember",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_Nama",
                table: "Members",
                column: "NamaLengkap");

            migrationBuilder.CreateIndex(
                name: "IX_Members_NoTelepon",
                table: "Members",
                column: "NoTelepon",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPlans_NamaPaket",
                table: "MembershipPlans",
                column: "NamaPaket",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PTAssignments_TrainerId",
                table: "PTAssignments",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "UX_PT_MemberAktif",
                table: "PTAssignments",
                column: "MemberId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Trainers_UserId",
                table: "Trainers",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_NoKuitansi",
                table: "Transactions",
                column: "NoKuitansi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PlanId",
                table: "Transactions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserIdKasir",
                table: "Transactions",
                column: "UserIdKasir");

            migrationBuilder.CreateIndex(
                name: "IX_Trx_Member",
                table: "Transactions",
                columns: new[] { "MemberId", "Status", "TglBerakhir" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            // Status member dihitung saat dibaca (PRD 7.3). Logika harus sama dengan MembershipCalculator.
            // Dibungkus EXEC karena CREATE VIEW harus jadi statement pertama dalam batch (skrip idempotent).
            const string createView = @"
CREATE VIEW vw_MemberStatus AS
SELECT m.MemberId, m.KodeMember, m.NamaLengkap, m.NoTelepon,
       t.TglBerakhirTerakhir,
       CASE
           WHEN m.IsActive = 0 THEN 'Nonaktif'
           WHEN t.TglBerakhirTerakhir IS NULL THEN 'Belum Berlangganan'
           WHEN CAST(GETDATE() AS DATE) > t.TglBerakhirTerakhir THEN 'Kedaluwarsa'
           WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), t.TglBerakhirTerakhir) < 7 THEN 'Akan Habis'
           ELSE 'Aktif'
       END AS Status
FROM Members m
OUTER APPLY (
    SELECT MAX(TglBerakhir) AS TglBerakhirTerakhir
    FROM Transactions
    WHERE MemberId = m.MemberId AND Status = 'Lunas'
) t;";
            migrationBuilder.Sql($"EXEC(N'{createView.Replace("'", "''")}')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_MemberStatus;");

            migrationBuilder.DropTable(
                name: "PTAssignments");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Trainers");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "MembershipPlans");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
