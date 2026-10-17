using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AgroTraceCR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Registros",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntidadId = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Red = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registros", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Productores",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Finca = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Productores_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductorId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecioReferencia = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Productos_Productores_ProductorId",
                        column: x => x.ProductorId,
                        principalTable: "Productores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Vacas",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Raza = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Finca = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductorId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vacas_Productores_ProductorId",
                        column: x => x.ProductorId,
                        principalTable: "Productores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Lotes",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Cantidad = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    FechaCosecha = table.Column<DateOnly>(type: "date", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lotes_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Certificados",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VacaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Alcance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificados_Vacas_VacaId",
                        column: x => x.VacaId,
                        principalTable: "Vacas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompradorId = table.Column<int>(type: "int", nullable: false),
                    LoteId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalTable: "Lotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Pedidos_Usuarios_CompradorId",
                        column: x => x.CompradorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Email", "Nombre", "Rol" },
                values: new object[,]
                {
                    { 1, "ana@example.test", "Ana Solís", 0 },
                    { 2, "luis@example.test", "Luis Mora", 0 },
                    { 3, "pyme@example.test", "Abasto Verde PYME", 1 },
                    { 4, "admin@example.test", "Admin demo", 2 },
                }
            );

            migrationBuilder.InsertData(
                table: "Productores",
                columns: new[] { "Id", "Finca", "Nombre", "Provincia", "UsuarioId" },
                values: new object[,]
                {
                    { 1, "El Roble", "Ana Solís", "Cartago", 1 },
                    { 2, "La Esperanza", "Luis Mora", "Alajuela", 2 },
                }
            );

            migrationBuilder.InsertData(
                table: "Productos",
                columns: new[] { "Id", "Nombre", "PrecioReferencia", "ProductorId", "Unidad" },
                values: new object[,]
                {
                    { 1, "Café de altura", 4500m, 1, "kg" },
                    { 2, "Aguacate", 1800m, 1, "kg" },
                    { 3, "Leche fresca", 550m, 2, "litro" },
                }
            );

            migrationBuilder.InsertData(
                table: "Vacas",
                columns: new[]
                {
                    "Id",
                    "Codigo",
                    "Estado",
                    "FechaNacimiento",
                    "Finca",
                    "ProductorId",
                    "Raza",
                },
                values: new object[,]
                {
                    {
                        1,
                        "BOV-CR-0001",
                        "Activa",
                        new DateOnly(2023, 3, 12),
                        "La Esperanza",
                        2,
                        "Brahman",
                    },
                    {
                        2,
                        "BOV-CR-0002",
                        "Activa",
                        new DateOnly(2024, 5, 20),
                        "El Roble",
                        1,
                        "Jersey",
                    },
                }
            );

            migrationBuilder.InsertData(
                table: "Lotes",
                columns: new[] { "Id", "Cantidad", "Codigo", "FechaCosecha", "ProductoId" },
                values: new object[,]
                {
                    { 1, 250m, "LOT-CR-0001", new DateOnly(2026, 10, 1), 1 },
                    { 2, 500m, "LOT-CR-0002", new DateOnly(2026, 10, 5), 2 },
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Certificados_VacaId",
                table: "Certificados",
                column: "VacaId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_Codigo",
                table: "Lotes",
                column: "Codigo",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_ProductoId",
                table: "Lotes",
                column: "ProductoId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_CompradorId",
                table: "Pedidos",
                column: "CompradorId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_LoteId",
                table: "Pedidos",
                column: "LoteId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Productores_UsuarioId",
                table: "Productores",
                column: "UsuarioId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Productos_ProductorId",
                table: "Productos",
                column: "ProductorId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Vacas_Codigo",
                table: "Vacas",
                column: "Codigo",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Vacas_ProductorId",
                table: "Vacas",
                column: "ProductorId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Certificados");

            migrationBuilder.DropTable(name: "Pedidos");

            migrationBuilder.DropTable(name: "Registros");

            migrationBuilder.DropTable(name: "Vacas");

            migrationBuilder.DropTable(name: "Lotes");

            migrationBuilder.DropTable(name: "Productos");

            migrationBuilder.DropTable(name: "Productores");

            migrationBuilder.DropTable(name: "Usuarios");
        }
    }
}
