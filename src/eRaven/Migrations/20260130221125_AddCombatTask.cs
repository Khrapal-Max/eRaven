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
                name: "combat_task_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    order_title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    recorded_at = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.CheckConstraint("ck_combat_task_documents_canceled", "status <> 2 OR canceled_at_utc IS NOT NULL");
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
                name: "timesheet_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    lane = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    end_mode = table.Column<int>(type: "integer", nullable: false),
                    end_date_meaning = table.Column<int>(type: "integer", nullable: false),
                    next_code_on_end = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_planning_cutoff = table.Column<bool>(type: "boolean", nullable: false),
                    planning_cutoff_shift_days = table.Column<int>(type: "integer", nullable: false),
                    is_terminal = table.Column<bool>(type: "boolean", nullable: false),
                    requires_reference = table.Column<bool>(type: "boolean", nullable: false),
                    requires_note = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "timesheet_timelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lane = table.Column<int>(type: "integer", nullable: false),
                    opened_at = table.Column<DateOnly>(type: "date", nullable: false),
                    closed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    closed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet_timelines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "combat_task_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_sequence = table.Column<int>(type: "integer", nullable: false),
                    source_doc_no = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    action_kind = table.Column<int>(type: "integer", nullable: false),
                    action_date = table.Column<DateOnly>(type: "date", nullable: false),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mission_display = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rnokpp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    full_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    rank = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    position = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    weapon = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    callsign = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_task_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_combat_task_entries_combat_task_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "combat_task_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "timesheet_code_transitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    lane = table.Column<int>(type: "integer", nullable: false),
                    from_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet_code_transitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timesheet_code_transitions_timesheet_codes_from_code_id",
                        column: x => x.from_code_id,
                        principalTable: "timesheet_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_timesheet_code_transitions_timesheet_codes_to_code_id",
                        column: x => x.to_code_id,
                        principalTable: "timesheet_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "timesheet_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    timeline_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lane = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    from = table.Column<DateOnly>(type: "date", nullable: false),
                    to = table.Column<DateOnly>(type: "date", nullable: true),
                    reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    note = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timesheet_entries_timesheet_timelines_timeline_id",
                        column: x => x.timeline_id,
                        principalTable: "timesheet_timelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_documents_order_title",
                table: "combat_task_documents",
                column: "order_title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_entries_document_id_group_id_person_id",
                table: "combat_task_entries",
                columns: new[] { "document_id", "group_id", "person_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_entries_document_id_group_sequence",
                table: "combat_task_entries",
                columns: new[] { "document_id", "group_sequence" });

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

            migrationBuilder.CreateIndex(
                name: "IX_timesheet_code_transitions_to_code_id",
                table: "timesheet_code_transitions",
                column: "to_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_ts_transitions_lane_from",
                table: "timesheet_code_transitions",
                columns: new[] { "lane", "from_code_id" });

            migrationBuilder.CreateIndex(
                name: "ux_ts_transitions_from_to",
                table: "timesheet_code_transitions",
                columns: new[] { "from_code_id", "to_code_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ts_codes_lane_code",
                table: "timesheet_codes",
                columns: new[] { "lane", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ts_entries_person_lane_range",
                table: "timesheet_entries",
                columns: new[] { "person_id", "lane", "from", "to" });

            migrationBuilder.CreateIndex(
                name: "ix_ts_entries_timeline_lane_from",
                table: "timesheet_entries",
                columns: new[] { "timeline_id", "lane", "from" });

            migrationBuilder.CreateIndex(
                name: "ix_ts_timeline_person_lane_closed",
                table: "timesheet_timelines",
                columns: new[] { "person_id", "lane", "closed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "combat_task_entries");

            migrationBuilder.DropTable(
                name: "missions");

            migrationBuilder.DropTable(
                name: "timesheet_code_transitions");

            migrationBuilder.DropTable(
                name: "timesheet_entries");

            migrationBuilder.DropTable(
                name: "combat_task_documents");

            migrationBuilder.DropTable(
                name: "timesheet_codes");

            migrationBuilder.DropTable(
                name: "timesheet_timelines");
        }
    }
}
