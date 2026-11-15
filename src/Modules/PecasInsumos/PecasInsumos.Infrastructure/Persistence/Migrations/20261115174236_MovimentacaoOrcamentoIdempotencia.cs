using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PecasInsumos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MovimentacaoOrcamentoIdempotencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "movimentacao_orcamento",
                schema: "pecas_insumos",
                columns: table => new
                {
                    orcamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    registrada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimentacao_orcamento", x => new { x.orcamento_id, x.tipo });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimentacao_orcamento",
                schema: "pecas_insumos");
        }
    }
}
