using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKS.DeepWater.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyMemberModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kolejność ma znaczenie: nowe kolumny muszą istnieć, a tabela Addresses jeszcze żyć,
            // żeby dało się przenieść adres. Dopiero potem usuwamy tabelę i zbędne kolumny.
            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Members",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "Members",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Members",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            // Przeniesienie adresu podstawowego (najniższy Type = zameldowania) na rekord członka.
            // Nr lokalu, kod pocztowy i kraj przepadają - spółka tych danych nie zbiera.
            migrationBuilder.Sql(@"
                UPDATE Members SET
                    Street = (SELECT a.Street FROM Addresses a
                              WHERE a.MemberId = Members.Id ORDER BY a.Type, a.Id LIMIT 1),
                    HouseNumber = (SELECT a.HouseNumber FROM Addresses a
                              WHERE a.MemberId = Members.Id ORDER BY a.Type, a.Id LIMIT 1),
                    City = (SELECT a.City FROM Addresses a
                              WHERE a.MemberId = Members.Id ORDER BY a.Type, a.Id LIMIT 1)
                WHERE EXISTS (SELECT 1 FROM Addresses a WHERE a.MemberId = Members.Id);");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "JoinDate",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LeaveDate",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "Members");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Members");

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "Members",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Members",
                type: "TEXT",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "JoinDate",
                table: "Members",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "LeaveDate",
                table: "Members",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "Members",
                type: "TEXT",
                maxLength: 11,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    ApartmentNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HouseNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Street = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_MemberId",
                table: "Addresses",
                column: "MemberId");
        }
    }
}
