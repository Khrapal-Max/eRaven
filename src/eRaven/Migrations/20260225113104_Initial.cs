using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "missions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_area = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    name_point = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    type_drone = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    target = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mission_mode = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateOnly>(type: "date", nullable: false),
                    closed_at = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_missions", x => x.id);
                    table.CheckConstraint("ck_missions_dates", "\"closed_at\" IS NULL OR \"closed_at\" >= \"created_at\"");
                });

            migrationBuilder.CreateTable(
                name: "person_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    event_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    author = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_events", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "person_read",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lifecycle = table.Column<int>(type: "integer", nullable: false),
                    enrollment_kind = table.Column<int>(type: "integer", nullable: true),
                    enrollment_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    rnokpp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    full_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    rank = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    position_sort = table.Column<int>(type: "integer", nullable: true),
                    position = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    bzvp = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    weapon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    callsign = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    enrolled_at = table.Column<DateOnly>(type: "date", nullable: true),
                    excluded_at = table.Column<DateOnly>(type: "date", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_read", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "timesheet_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_terminal = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    role_code = table.Column<byte>(type: "smallint", nullable: false),
                    ui_style = table.Column<byte>(type: "smallint", nullable: false),
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
                name: "timesheet_code_transitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_shift_days = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet_code_transitions", x => x.id);
                    table.CheckConstraint("ck_timesheet_code_transitions_start_shift_days", "\"start_shift_days\" IN (0,1)");
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

            migrationBuilder.CreateIndex(
                name: "ix_missions_area_created_closed",
                table: "missions",
                columns: new[] { "position_area", "created_at", "closed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_missions_mode",
                table: "missions",
                column: "mission_mode");

            migrationBuilder.CreateIndex(
                name: "ix_missions_position_area",
                table: "missions",
                column: "position_area");

            migrationBuilder.CreateIndex(
                name: "ux_missions_active_key",
                table: "missions",
                columns: new[] { "position_area", "name_point", "mission_mode", "target" },
                unique: true,
                filter: "\"closed_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_events_aggregate_effective_date",
                table: "person_events",
                columns: new[] { "aggregate_id", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "ix_person_events_aggregate_version",
                table: "person_events",
                columns: new[] { "aggregate_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_events_event_type",
                table: "person_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_person_read_enrolled_excluded",
                table: "person_read",
                columns: new[] { "enrolled_at", "excluded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_person_read_kind_possort_last",
                table: "person_read",
                columns: new[] { "enrollment_kind", "position_sort", "last_name" });

            migrationBuilder.CreateIndex(
                name: "ix_person_read_lifecycle",
                table: "person_read",
                column: "lifecycle");

            migrationBuilder.CreateIndex(
                name: "ux_person_read_rnokpp",
                table: "person_read",
                column: "rnokpp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_timesheet_code_transitions_to_code_id",
                table: "timesheet_code_transitions",
                column: "to_code_id");

            migrationBuilder.CreateIndex(
                name: "ux_timesheet_code_transitions_from_to",
                table: "timesheet_code_transitions",
                columns: new[] { "from_code_id", "to_code_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_timesheet_codes_active",
                table: "timesheet_codes",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_timesheet_codes_code",
                table: "timesheet_codes",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "missions");

            migrationBuilder.DropTable(
                name: "person_events");

            migrationBuilder.DropTable(
                name: "person_read");

            migrationBuilder.DropTable(
                name: "timesheet_code_transitions");

            migrationBuilder.DropTable(
                name: "timesheet_codes");
        }
    }
}
