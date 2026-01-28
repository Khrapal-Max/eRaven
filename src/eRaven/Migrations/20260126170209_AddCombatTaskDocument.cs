using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatTaskDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "combat_task_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    recorded_at = table.Column<DateOnly>(type: "date", nullable: false),
                    order = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    canceled_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updates_by = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_by = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_task_documents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_documents_recorded_at",
                table: "combat_task_documents",
                column: "recorded_at");

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_documents_recorded_at_status",
                table: "combat_task_documents",
                columns: new[] { "recorded_at", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "combat_task_documents");
        }
    }
}
