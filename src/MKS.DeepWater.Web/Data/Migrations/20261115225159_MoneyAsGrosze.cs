using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKS.DeepWater.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoneyAsGrosze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AmountPerShare",
                table: "FeeRates",
                newName: "AmountPerShareGrosze");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "FeePayments",
                newName: "AmountGrosze");

            // Przeliczenie złotych na grosze PRZED zmianą typu. Sama zmiana typu przepisałaby
            // 480.0 jako 480, czyli po odczycie 4,80 zł. ROUND zbiera błąd zapisu double
            // (np. 0.29 * 100 = 28.999999999999996).
            migrationBuilder.Sql("UPDATE FeeRates SET AmountPerShareGrosze = ROUND(AmountPerShareGrosze * 100);");
            migrationBuilder.Sql("UPDATE FeePayments SET AmountGrosze = ROUND(AmountGrosze * 100);");

            migrationBuilder.AlterColumn<long>(
                name: "AmountPerShareGrosze",
                table: "FeeRates",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<long>(
                name: "AmountGrosze",
                table: "FeePayments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AmountPerShareGrosze",
                table: "FeeRates",
                newName: "AmountPerShare");

            migrationBuilder.RenameColumn(
                name: "AmountGrosze",
                table: "FeePayments",
                newName: "Amount");

            migrationBuilder.AlterColumn<double>(
                name: "AmountPerShare",
                table: "FeeRates",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "Amount",
                table: "FeePayments",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.Sql("UPDATE FeeRates SET AmountPerShare = AmountPerShare / 100.0;");
            migrationBuilder.Sql("UPDATE FeePayments SET Amount = Amount / 100.0;");
        }
    }
}
