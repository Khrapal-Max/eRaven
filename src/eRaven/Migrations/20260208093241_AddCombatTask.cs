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
            migrationBuilder.AddColumn<int>(
                name: "position_sort",
                table: "person_read",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "combat_task_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    order_title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
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
                    source_end_details_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mission_assignments", x => x.id);
                    table.CheckConstraint("ck_mission_assignments_to_gte_from", "to_date IS NULL OR to_date >= from_date");
                });

            migrationBuilder.CreateTable(
                name: "missions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_area = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    name_point = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false, defaultValue: ""),
                    type_drone = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Target = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mission_mode = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateOnly>(type: "date", nullable: false),
                    closed_at = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_missions", x => x.Id);
                    table.CheckConstraint("ck_missions_dates", "\"closed_at\" IS NULL OR \"closed_at\" >= \"created_at\"");
                });

            migrationBuilder.CreateTable(
                name: "timesheet_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_timesheet_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "timesheet_timelines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_at = table.Column<DateOnly>(type: "date", nullable: false),
                    closed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    closed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet_timelines", x => x.id);
                    table.CheckConstraint("ck_ts_timelines_closed_gte_opened", "closed_at IS NULL OR closed_at >= opened_at");
                });

            migrationBuilder.CreateTable(
                name: "combat_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combat_task_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "timesheet_code_transitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet_code_transitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_timesheet_code_transitions_timesheet_codes_from_code_id",
                        column: x => x.from_code_id,
                        principalTable: "timesheet_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_timesheet_code_transitions_timesheet_codes_to_code_id",
                        column: x => x.to_code_id,
                        principalTable: "timesheet_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "timesheet_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timeline_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_timesheet_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_timesheet_entries_timesheet_timelines_timeline_id",
                        column: x => x.timeline_id,
                        principalTable: "timesheet_timelines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "combat_task_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combat_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    effective_at = table.Column<DateOnly>(type: "date", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rnokpp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    full_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    callsign = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combat_task_lines", x => x.id);
                    table.CheckConstraint("ck_combat_task_lines_kind_valid", "kind IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_combat_task_lines_combat_tasks_combat_task_id",
                        column: x => x.combat_task_id,
                        principalTable: "combat_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_person_read_kind_possort_last",
                table: "person_read",
                columns: new[] { "enrollment_kind", "position_sort", "last_name" });

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_documents_order_title",
                table: "combat_task_documents",
                column: "order_title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_documents_recorded_at",
                table: "combat_task_documents",
                column: "recorded_at");

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_documents_status",
                table: "combat_task_documents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_lines_combat_task_id",
                table: "combat_task_lines",
                column: "combat_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_lines_combat_task_id_person_id_kind_effective_at",
                table: "combat_task_lines",
                columns: new[] { "combat_task_id", "person_id", "kind", "effective_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_lines_kind",
                table: "combat_task_lines",
                column: "kind");

            migrationBuilder.CreateIndex(
                name: "IX_combat_task_lines_person_id_effective_at",
                table: "combat_task_lines",
                columns: new[] { "person_id", "effective_at" });

            migrationBuilder.CreateIndex(
                name: "IX_combat_tasks_combat_task_document_id",
                table: "combat_tasks",
                column: "combat_task_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_combat_tasks_mission_id",
                table: "combat_tasks",
                column: "mission_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_mission_id_from_date_to_date",
                table: "mission_assignments",
                columns: new[] { "mission_id", "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_person_id",
                table: "mission_assignments",
                column: "person_id",
                unique: true,
                filter: "to_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_person_id_from_date_to_date",
                table: "mission_assignments",
                columns: new[] { "person_id", "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_source_start_details_id",
                table: "mission_assignments",
                column: "source_start_details_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_missions_mission_mode",
                table: "missions",
                column: "mission_mode");

            migrationBuilder.CreateIndex(
                name: "IX_missions_position_area",
                table: "missions",
                column: "position_area");

            migrationBuilder.CreateIndex(
                name: "IX_missions_position_area_created_at_closed_at",
                table: "missions",
                columns: new[] { "position_area", "created_at", "closed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_missions_position_area_name_point_mission_mode_Target",
                table: "missions",
                columns: new[] { "position_area", "name_point", "mission_mode", "Target" },
                unique: true,
                filter: "\"closed_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_timesheet_code_transitions_to_code_id",
                table: "timesheet_code_transitions",
                column: "to_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_ts_transitions_from",
                table: "timesheet_code_transitions",
                column: "from_code_id");

            migrationBuilder.CreateIndex(
                name: "ux_ts_transitions_from_to",
                table: "timesheet_code_transitions",
                columns: new[] { "from_code_id", "to_code_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ts_codes_code",
                table: "timesheet_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ts_entries_person_range",
                table: "timesheet_entries",
                columns: new[] { "person_id", "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "ix_ts_entries_timeline_from",
                table: "timesheet_entries",
                columns: new[] { "timeline_id", "from_date" });

            migrationBuilder.CreateIndex(
                name: "ix_ts_timelines_person_closed",
                table: "timesheet_timelines",
                columns: new[] { "person_id", "closed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ts_timelines_person_opened",
                table: "timesheet_timelines",
                columns: new[] { "person_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ux_ts_timelines_person_active",
                table: "timesheet_timelines",
                column: "person_id",
                unique: true,
                filter: "closed_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "combat_task_lines");

            migrationBuilder.DropTable(
                name: "mission_assignments");

            migrationBuilder.DropTable(
                name: "timesheet_code_transitions");

            migrationBuilder.DropTable(
                name: "timesheet_entries");

            migrationBuilder.DropTable(
                name: "combat_tasks");

            migrationBuilder.DropTable(
                name: "timesheet_codes");

            migrationBuilder.DropTable(
                name: "timesheet_timelines");

            migrationBuilder.DropTable(
                name: "combat_task_documents");

            migrationBuilder.DropTable(
                name: "missions");

            migrationBuilder.DropIndex(
                name: "ix_person_read_kind_possort_last",
                table: "person_read");

            migrationBuilder.DropColumn(
                name: "position_sort",
                table: "person_read");
        }
    }
}
