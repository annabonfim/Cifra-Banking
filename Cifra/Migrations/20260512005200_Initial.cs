using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cifra.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CIFRA_AGENCIA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Codigo = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Endereco = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    Cidade = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    Uf = table.Column<string>(type: "NVARCHAR2(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CIFRA_AGENCIA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CIFRA_PRODUTO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Ativo = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    TIPO_PRODUTO = table.Column<string>(type: "NVARCHAR2(21)", maxLength: 21, nullable: false),
                    MODELO_EQUIPAMENTO = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    MDR_BASE_DEBITO = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: true),
                    MDR_BASE_CREDITO_VISTA = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: true),
                    MDR_BASE_CREDITO_PARC = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: true),
                    FATURAMENTO_MIN = table.Column<decimal>(type: "DECIMAL(14,2)", precision: 14, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CIFRA_PRODUTO", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CIFRA_CLIENTE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Telefone = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AgenciaId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TIPO_CLIENTE = table.Column<string>(type: "NVARCHAR2(8)", maxLength: 8, nullable: false),
                    CPF = table.Column<string>(type: "NVARCHAR2(11)", maxLength: 11, nullable: true),
                    DATA_NASCIMENTO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    CNPJ = table.Column<string>(type: "NVARCHAR2(14)", maxLength: 14, nullable: true),
                    RAZAO_SOCIAL = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CIFRA_CLIENTE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CIFRA_CLIENTE_CIFRA_AGENCIA_AgenciaId",
                        column: x => x.AgenciaId,
                        principalTable: "CIFRA_AGENCIA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CIFRA_CONTRATACAO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ClienteId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DataSolicitacao = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DataProcessamento = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    FaturamentoMensalEstimado = table.Column<decimal>(type: "DECIMAL(14,2)", precision: 14, scale: 2, nullable: false),
                    ScoreRisco = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    MdrAplicado = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    MotivoRecusa = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CIFRA_CONTRATACAO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CIFRA_CONTRATACAO_CIFRA_CLIENTE_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "CIFRA_CLIENTE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CIFRA_CONTRATACAO_CIFRA_PRODUTO_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "CIFRA_PRODUTO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CIFRA_AGENCIA_Codigo",
                table: "CIFRA_AGENCIA",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CIFRA_CLIENTE_AgenciaId",
                table: "CIFRA_CLIENTE",
                column: "AgenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_CIFRA_CLIENTE_CNPJ",
                table: "CIFRA_CLIENTE",
                column: "CNPJ",
                unique: true,
                filter: "\"CNPJ\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CIFRA_CLIENTE_CPF",
                table: "CIFRA_CLIENTE",
                column: "CPF",
                unique: true,
                filter: "\"CPF\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CIFRA_CONTRATACAO_ClienteId",
                table: "CIFRA_CONTRATACAO",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_CIFRA_CONTRATACAO_ProdutoId",
                table: "CIFRA_CONTRATACAO",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CIFRA_CONTRATACAO");

            migrationBuilder.DropTable(
                name: "CIFRA_CLIENTE");

            migrationBuilder.DropTable(
                name: "CIFRA_PRODUTO");

            migrationBuilder.DropTable(
                name: "CIFRA_AGENCIA");
        }
    }
}
