using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroTraceCR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SorobanEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContractId",
                table: "Registros",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "ContractVersion",
                table: "Registros",
                type: "int",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ContractId", table: "Registros");

            migrationBuilder.DropColumn(name: "ContractVersion", table: "Registros");
        }
    }
}
