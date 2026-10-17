using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroTraceCR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CheckoutTestnet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cuenta = table.Column<string>(
                        type: "nvarchar(56)",
                        maxLength: 56,
                        nullable: false
                    ),
                    Destino = table.Column<string>(
                        type: "nvarchar(56)",
                        maxLength: 56,
                        nullable: false
                    ),
                    Memo = table.Column<string>(
                        type: "nvarchar(28)",
                        maxLength: 28,
                        nullable: false
                    ),
                    DetallesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalColones = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    ColonesPorXlm = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    MontoXlm = table.Column<decimal>(
                        type: "decimal(18,7)",
                        precision: 18,
                        scale: 7,
                        nullable: false
                    ),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Vence = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    TransactionId = table.Column<string>(
                        type: "nvarchar(64)",
                        maxLength: 64,
                        nullable: true
                    ),
                    Version = table.Column<byte[]>(
                        type: "rowversion",
                        rowVersion: true,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "ConfiguracionPagos",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Destino = table.Column<string>(
                        type: "nvarchar(56)",
                        maxLength: 56,
                        nullable: false
                    ),
                    ColonesPorXlm = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionPagos", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Memo",
                table: "Compras",
                column: "Memo",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Compras_TransactionId",
                table: "Compras",
                column: "TransactionId",
                unique: true,
                filter: "[TransactionId] IS NOT NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Compras");

            migrationBuilder.DropTable(name: "ConfiguracionPagos");
        }
    }
}
