using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddTimesheetAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "timesheet_aggregates",
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
                    table.PrimaryKey("PK_timesheet_aggregates", x => x.id);
                    table.CheckConstraint("ck_ts_aggregates_closed_gte_opened", "closed_at IS NULL OR closed_at >= opened_at");
                });

            migrationBuilder.CreateTable(
                name: "timesheet_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timesheet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timesheet_codedefinition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: true),
                    reference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    note = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet_entries", x => x.id);
                    table.CheckConstraint("ck_timesheet_entries_to_gt_from", "to_date IS NULL OR to_date > from_date");
                    table.ForeignKey(
                        name: "FK_timesheet_entries_timesheet_aggregates_timesheet_id",
                        column: x => x.timesheet_id,
                        principalTable: "timesheet_aggregates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_timesheet_entries_timesheet_codes_timesheet_codedefinition_~",
                        column: x => x.timesheet_codedefinition_id,
                        principalTable: "timesheet_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ts_aggregates_person_closed",
                table: "timesheet_aggregates",
                columns: new[] { "person_id", "closed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ts_aggregates_person_opened",
                table: "timesheet_aggregates",
                columns: new[] { "person_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ux_ts_aggregates_person_active",
                table: "timesheet_aggregates",
                column: "person_id",
                unique: true,
                filter: "closed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_timesheet_entries_timesheet_codedefinition_id",
                table: "timesheet_entries",
                column: "timesheet_codedefinition_id");

            migrationBuilder.CreateIndex(
                name: "ix_ts_entries_person_range",
                table: "timesheet_entries",
                columns: new[] { "person_id", "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "ux_ts_entries_timesheet_from_active",
                table: "timesheet_entries",
                columns: new[] { "timesheet_id", "from_date" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "timesheet_entries");

            migrationBuilder.DropTable(
                name: "timesheet_aggregates");
        }
    }
}
