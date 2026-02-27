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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    order_title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    recorded_at = table.Column<DateOnly>(type: "date", nullable: false),
                    canceled_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    canceled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_task_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mission_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: true),
                    source_start_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_start_details_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_end_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_end_details_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_assignments", x => x.id);
                    table.CheckConstraint("ck_mission_assignments_end_source_consistency", "(to_date IS NULL AND source_end_document_id IS NULL AND source_end_details_id IS NULL) OR (to_date IS NOT NULL AND source_end_document_id IS NOT NULL AND source_end_details_id IS NOT NULL)");
                    table.CheckConstraint("ck_mission_assignments_to_gt_from", "to_date IS NULL OR to_date > from_date");
                });

            migrationBuilder.CreateTable(
                name: "combat_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combat_task_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_document = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_combat_tasks_combat_task_documents_combat_task_document_id",
                        column: x => x.combat_task_document_id,
                        principalTable: "combat_task_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_combat_tasks_missions_mission_id",
                        column: x => x.mission_id,
                        principalTable: "missions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "combat_task_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combat_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    effective_at = table.Column<DateOnly>(type: "date", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rnokpp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    full_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    rank = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    position = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    weapon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    callsign = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_task_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_combat_task_details_combat_tasks_combat_task_id",
                        column: x => x.combat_task_id,
                        principalTable: "combat_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_combat_task_details_person_date",
                table: "combat_task_details",
                columns: new[] { "person_id", "effective_at" });

            migrationBuilder.CreateIndex(
                name: "ix_combat_task_details_task",
                table: "combat_task_details",
                column: "combat_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_combat_task_documents_order_title",
                table: "combat_task_documents",
                column: "order_title");

            migrationBuilder.CreateIndex(
                name: "ix_combat_task_documents_recorded_at",
                table: "combat_task_documents",
                column: "recorded_at");

            migrationBuilder.CreateIndex(
                name: "ix_combat_task_documents_status",
                table: "combat_task_documents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_combat_tasks_document",
                table: "combat_tasks",
                column: "combat_task_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_combat_tasks_mission",
                table: "combat_tasks",
                column: "mission_id");

            migrationBuilder.CreateIndex(
                name: "ix_mission_assignments_enddoc_mission",
                table: "mission_assignments",
                columns: new[] { "source_end_document_id", "mission_id" });

            migrationBuilder.CreateIndex(
                name: "ix_mission_assignments_mission_person_from",
                table: "mission_assignments",
                columns: new[] { "mission_id", "person_id", "from_date" });

            migrationBuilder.CreateIndex(
                name: "ix_mission_assignments_startdoc_mission",
                table: "mission_assignments",
                columns: new[] { "source_start_document_id", "mission_id" });

            migrationBuilder.CreateIndex(
                name: "ux_mission_assignments_end_details",
                table: "mission_assignments",
                column: "source_end_details_id",
                unique: true,
                filter: "source_end_details_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_mission_assignments_person_open",
                table: "mission_assignments",
                column: "person_id",
                unique: true,
                filter: "to_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_mission_assignments_start_details",
                table: "mission_assignments",
                column: "source_start_details_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mission_assignments_startdoc_mission_person_from",
                table: "mission_assignments",
                columns: new[] { "source_start_document_id", "mission_id", "person_id", "from_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "combat_task_details");

            migrationBuilder.DropTable(
                name: "mission_assignments");

            migrationBuilder.DropTable(
                name: "combat_tasks");

            migrationBuilder.DropTable(
                name: "combat_task_documents");
        }
    }
}
