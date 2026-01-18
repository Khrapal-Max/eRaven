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
                name: "ix_person_read_lifecycle",
                table: "person_read",
                column: "lifecycle");

            migrationBuilder.CreateIndex(
                name: "ux_person_read_rnokpp",
                table: "person_read",
                column: "rnokpp",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person_events");

            migrationBuilder.DropTable(
                name: "person_read");
        }
    }
}
