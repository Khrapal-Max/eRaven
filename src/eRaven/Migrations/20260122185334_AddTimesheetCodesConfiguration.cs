using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddTimesheetCodesConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "timesheet_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    lane = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    end_mode = table.Column<int>(type: "integer", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "timesheet_code_transitions");

            migrationBuilder.DropTable(
                name: "timesheet_codes");
        }
    }
}
