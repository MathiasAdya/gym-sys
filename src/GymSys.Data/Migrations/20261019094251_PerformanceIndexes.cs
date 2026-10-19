using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymSys.Data.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Trx_Tgl",
                table: "Transactions",
                column: "TglTransaksi")
                .Annotation("SqlServer:Include", new[] { "Status", "TotalBayar" });

            migrationBuilder.CreateIndex(
                name: "IX_Members_TglBergabung",
                table: "Members",
                column: "TglBergabung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trx_Tgl",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Members_TglBergabung",
                table: "Members");
        }
    }
}
