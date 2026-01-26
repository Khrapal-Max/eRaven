using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "combat_task_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    EndedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    StartDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlanningDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlanningDocTitle = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    PositionalArea = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    GroupName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    AssetType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Goal = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActual = table.Column<bool>(type: "boolean", nullable: false),
                    RNOKPP = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rank = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Weapon = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Callsign = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_task_assignments", x => x.Id);
                    table.CheckConstraint("ck_combat_task_assignment_dates", "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "combat_task_plan_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    PlanningDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlanningDocTitle = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceledReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CanceledBy = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CanceledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_task_plan_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "combat_task_plan_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RNOKPP = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rank = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Weapon = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Callsign = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PositionalArea = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    GroupName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    AssetType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Goal = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActual = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_task_plan_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_combat_task_plan_lines_combat_task_plan_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "combat_task_plan_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_assignments_PersonId",
                table: "combat_task_assignments",
                column: "PersonId",
                unique: true,
                filter: "\"EndedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_assignments_PersonId_StartedAt",
                table: "combat_task_assignments",
                columns: new[] { "PersonId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_assignments_PlanningDate_PersonId",
                table: "combat_task_assignments",
                columns: new[] { "PlanningDate", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_plan_documents_PlanningDate_Status",
                table: "combat_task_plan_documents",
                columns: new[] { "PlanningDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_plan_documents_RecordedAt",
                table: "combat_task_plan_documents",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_plan_lines_AssignmentId",
                table: "combat_task_plan_lines",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_plan_lines_DocumentId_PersonId_Kind",
                table: "combat_task_plan_lines",
                columns: new[] { "DocumentId", "PersonId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_plan_lines_PersonId_ActionDate",
                table: "combat_task_plan_lines",
                columns: new[] { "PersonId", "ActionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "combat_task_assignments");

            migrationBuilder.DropTable(
                name: "combat_task_plan_lines");

            migrationBuilder.DropTable(
                name: "combat_task_plan_documents");
        }
    }
}
