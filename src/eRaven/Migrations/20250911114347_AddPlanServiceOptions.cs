using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanServiceOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plan_service_options",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dispatch_status_kind_id = table.Column<int>(type: "integer", nullable: true),
                    return_status_kind_id = table.Column<int>(type: "integer", nullable: true),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, defaultValue: "system"),
                    modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_service_options", x => x.id);
                    table.CheckConstraint("ck_plan_opts_dispatch_ne_return", "(dispatch_status_kind_id IS NULL) OR (return_status_kind_id IS NULL) OR (dispatch_status_kind_id <> return_status_kind_id)");
                    table.ForeignKey(
                        name: "FK_plan_service_options_status_kinds_dispatch_status_kind_id",
                        column: x => x.dispatch_status_kind_id,
                        principalTable: "status_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plan_service_options_status_kinds_return_status_kind_id",
                        column: x => x.return_status_kind_id,
                        principalTable: "status_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "plan_service_options",
                columns: new[] { "id", "author", "dispatch_status_kind_id", "modified_utc", "return_status_kind_id" },
                values: new object[] { 1, "system", 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.CreateIndex(
                name: "ix_plan_opts_dispatch_kind",
                table: "plan_service_options",
                column: "dispatch_status_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_opts_return_kind",
                table: "plan_service_options",
                column: "return_status_kind_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_service_options");
        }
    }
}
