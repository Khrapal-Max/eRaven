using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "combat_task_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    order_title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    RecordedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    canceled_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    canceled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_task_documents", x => x.Id);
                    table.CheckConstraint("ck_combat_task_documents_canceled", "\"Status\" <> 2 OR \"canceled_at_utc\" IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "mission_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    source_doc_no = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    action_kind = table.Column<int>(type: "integer", nullable: false),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "missions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionArea = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    NamePoint = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false, defaultValue: ""),
                    TypeDrone = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Target = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MissionMode = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosedAt = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_missions", x => x.Id);
                    table.CheckConstraint("ck_missions_dates", "\"ClosedAt\" IS NULL OR \"ClosedAt\" >= \"CreatedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "mission_action_persons",
                columns: table => new
                {
                    action_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rnokpp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    full_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    rank = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    weapon = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    callsign = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_action_persons", x => new { x.action_id, x.person_id });
                    table.ForeignKey(
                        name: "FK_mission_action_persons_mission_actions_action_id",
                        column: x => x.action_id,
                        principalTable: "mission_actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mission_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateOnly>(type: "date", nullable: false),
                    ended_at = table.Column<DateOnly>(type: "date", nullable: true),
                    start_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    end_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_action_id = table.Column<Guid>(type: "uuid", nullable: false),
                    end_action_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_assignments", x => x.Id);
                    table.CheckConstraint("ck_mission_assignments_dates", "\"ended_at\" IS NULL OR \"ended_at\" >= \"started_at\"");
                    table.ForeignKey(
                        name: "FK_mission_assignments_combat_task_documents_end_document_id",
                        column: x => x.end_document_id,
                        principalTable: "combat_task_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_assignments_combat_task_documents_start_document_id",
                        column: x => x.start_document_id,
                        principalTable: "combat_task_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_assignments_mission_actions_end_action_id",
                        column: x => x.end_action_id,
                        principalTable: "mission_actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mission_assignments_mission_actions_start_action_id",
                        column: x => x.start_action_id,
                        principalTable: "mission_actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_documents_order_title",
                table: "combat_task_documents",
                column: "order_title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mission_action_persons_action_id",
                table: "mission_action_persons",
                column: "action_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_action_persons_person_id",
                table: "mission_action_persons",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_actions_document_id_action_date",
                table: "mission_actions",
                columns: new[] { "document_id", "action_date" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_actions_document_id_sequence",
                table: "mission_actions",
                columns: new[] { "document_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_end_action_id",
                table: "mission_assignments",
                column: "end_action_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_end_document_id",
                table: "mission_assignments",
                column: "end_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_mission_id_started_at",
                table: "mission_assignments",
                columns: new[] { "mission_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_person_id",
                table: "mission_assignments",
                column: "person_id",
                unique: true,
                filter: "\"ended_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_person_id_started_at",
                table: "mission_assignments",
                columns: new[] { "person_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_start_action_id",
                table: "mission_assignments",
                column: "start_action_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_start_document_id",
                table: "mission_assignments",
                column: "start_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_missions_MissionMode",
                table: "missions",
                column: "MissionMode");

            migrationBuilder.CreateIndex(
                name: "IX_missions_PositionArea",
                table: "missions",
                column: "PositionArea");

            migrationBuilder.CreateIndex(
                name: "IX_missions_PositionArea_CreatedAt_ClosedAt",
                table: "missions",
                columns: new[] { "PositionArea", "CreatedAt", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_missions_PositionArea_NamePoint_MissionMode_Target",
                table: "missions",
                columns: new[] { "PositionArea", "NamePoint", "MissionMode", "Target" },
                unique: true,
                filter: "\"ClosedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mission_action_persons");

            migrationBuilder.DropTable(
                name: "mission_assignments");

            migrationBuilder.DropTable(
                name: "missions");

            migrationBuilder.DropTable(
                name: "combat_task_documents");

            migrationBuilder.DropTable(
                name: "mission_actions");
        }
    }
}
