using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

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
                name: "position_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    short_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    org_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    special_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_position_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, defaultValue: "system"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_kinds", x => x.id);
                    table.CheckConstraint("ck_status_kinds_code_not_blank", "length(trim(code)) > 0");
                    table.CheckConstraint("ck_status_kinds_name_not_blank", "length(trim(name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rnokpp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    rank = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    bzvp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    weapon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    callsign = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    position_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_kind_id = table.Column<int>(type: "integer", nullable: true),
                    is_attached = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    attached_from_unit = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persons", x => x.id);
                    table.ForeignKey(
                        name: "FK_persons_position_units_position_unit_id",
                        column: x => x.position_unit_id,
                        principalTable: "position_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_persons_status_kinds_status_kind_id",
                        column: x => x.status_kind_id,
                        principalTable: "status_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "status_transitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    from_status_kind_id = table.Column<int>(type: "integer", nullable: false),
                    to_status_kind_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_transitions", x => x.id);
                    table.CheckConstraint("ck_status_transitions_from_ne_to", "from_status_kind_id <> to_status_kind_id");
                    table.ForeignKey(
                        name: "FK_status_transitions_status_kinds_from_status_kind_id",
                        column: x => x.from_status_kind_id,
                        principalTable: "status_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_status_transitions_status_kinds_to_status_kind_id",
                        column: x => x.to_status_kind_id,
                        principalTable: "status_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "person_position_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    open_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    close_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, defaultValue: "system"),
                    modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_position_assignments", x => x.id);
                    table.CheckConstraint("ck_person_position_assignments_dates", "(close_utc IS NULL) OR (close_utc > open_utc)");
                    table.ForeignKey(
                        name: "FK_person_position_assignments_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_person_position_assignments_position_units_position_unit_id",
                        column: x => x.position_unit_id,
                        principalTable: "position_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "person_statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status_kind_id = table.Column<int>(type: "integer", nullable: false),
                    open_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sequence = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, defaultValue: "system"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_document_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_statuses", x => x.id);
                    table.ForeignKey(
                        name: "FK_person_statuses_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_person_statuses_status_kinds_status_kind_id",
                        column: x => x.status_kind_id,
                        principalTable: "status_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_action_name = table.Column<string>(type: "text", nullable: false),
                    effective_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    to_status_kind_id = table.Column<int>(type: "integer", nullable: true),
                    order_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    action_state = table.Column<short>(type: "smallint", nullable: false),
                    move_type = table.Column<short>(type: "smallint", nullable: false),
                    location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    group_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    crew_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    rnokpp = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    rank_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    position_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    bzvp = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    weapon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    callsign = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status_kind_on_date = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_plan_actions_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "status_kinds",
                columns: new[] { "id", "author", "code", "is_active", "modified", "name", "order" },
                values: new object[,]
                {
                    { 1, "system", "30", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "В районі", 10 },
                    { 2, "system", "100", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "В БР", 100 },
                    { 3, "system", "нб", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Переміщення", 50 },
                    { 4, "system", "нб", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Звільнення", 50 },
                    { 5, "system", "РОЗПОР", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Розпорядження", 40 },
                    { 6, "system", "100", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "В БТГр", 100 },
                    { 7, "system", "ВДР", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Відрядження", 80 },
                    { 8, "system", "ВДР", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Фахове навчання", 80 },
                    { 9, "system", "В", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Відпустка", 90 },
                    { 10, "system", "Л_Х", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Проходження ВЛК", 120 },
                    { 11, "system", "Л_Х", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Направлення на МСЕК", 120 },
                    { 12, "system", "Л_Х", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Лікування по хворобі", 120 },
                    { 13, "system", "Л_Б", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Лікування по пораненню", 130 },
                    { 14, "system", "БВ", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Безвісті", 170 },
                    { 15, "system", "П", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Полон", 175 },
                    { 16, "system", "200", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Загибель", 190 },
                    { 17, "system", "А", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Арешт", 178 },
                    { 18, "system", "СЗЧ", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "СЗЧ", 180 }
                });

            migrationBuilder.InsertData(
                table: "status_transitions",
                columns: new[] { "id", "from_status_kind_id", "to_status_kind_id" },
                values: new object[,]
                {
                    { 1, 1, 2 },
                    { 2, 1, 3 },
                    { 3, 1, 4 },
                    { 4, 1, 5 },
                    { 5, 1, 6 },
                    { 6, 1, 7 },
                    { 7, 1, 8 },
                    { 8, 1, 9 },
                    { 9, 1, 10 },
                    { 10, 1, 11 },
                    { 11, 1, 12 },
                    { 12, 1, 13 },
                    { 13, 1, 14 },
                    { 14, 1, 15 },
                    { 15, 1, 16 },
                    { 16, 1, 17 },
                    { 17, 1, 18 },
                    { 18, 2, 1 },
                    { 19, 2, 12 },
                    { 20, 2, 13 },
                    { 21, 2, 14 },
                    { 22, 2, 15 },
                    { 23, 2, 16 },
                    { 24, 2, 18 },
                    { 25, 3, 1 },
                    { 26, 3, 12 },
                    { 27, 3, 13 },
                    { 28, 3, 14 },
                    { 29, 3, 15 },
                    { 30, 3, 16 },
                    { 31, 3, 18 },
                    { 32, 4, 1 },
                    { 33, 5, 1 },
                    { 34, 6, 1 },
                    { 35, 7, 1 },
                    { 36, 7, 18 },
                    { 37, 8, 1 },
                    { 38, 8, 18 },
                    { 39, 9, 1 },
                    { 40, 9, 18 },
                    { 41, 10, 1 },
                    { 42, 10, 6 },
                    { 43, 10, 18 },
                    { 44, 11, 1 },
                    { 45, 11, 6 },
                    { 46, 11, 18 },
                    { 47, 12, 1 },
                    { 48, 12, 6 },
                    { 49, 12, 18 },
                    { 50, 13, 1 },
                    { 51, 13, 6 },
                    { 52, 13, 18 },
                    { 53, 14, 1 },
                    { 54, 14, 6 },
                    { 55, 15, 1 },
                    { 56, 15, 6 },
                    { 57, 18, 1 },
                    { 58, 18, 6 },
                    { 59, 17, 1 },
                    { 60, 17, 6 },
                    { 61, 16, 5 },
                    { 62, 16, 6 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_ppassign_person_close",
                table: "person_position_assignments",
                columns: new[] { "person_id", "close_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_ppassign_person_open",
                table: "person_position_assignments",
                columns: new[] { "person_id", "open_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_ppassign_position_open",
                table: "person_position_assignments",
                columns: new[] { "position_unit_id", "open_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_ppassign_person_open",
                table: "person_position_assignments",
                column: "person_id",
                unique: true,
                filter: "close_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ppassign_position_open",
                table: "person_position_assignments",
                column: "position_unit_id",
                unique: true,
                filter: "close_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_statuses_person_active_open",
                table: "person_statuses",
                columns: new[] { "person_id", "is_active", "open_date" });

            migrationBuilder.CreateIndex(
                name: "ix_person_statuses_person_open",
                table: "person_statuses",
                columns: new[] { "person_id", "open_date" });

            migrationBuilder.CreateIndex(
                name: "ix_person_statuses_source_document",
                table: "person_statuses",
                columns: new[] { "source_document_type", "source_document_id" });

            migrationBuilder.CreateIndex(
                name: "IX_person_statuses_status_kind_id",
                table: "person_statuses",
                column: "status_kind_id");

            migrationBuilder.CreateIndex(
                name: "ux_person_statuses_person_open_seq_active",
                table: "person_statuses",
                columns: new[] { "person_id", "open_date", "sequence" },
                unique: true,
                filter: "is_active = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_persons_fullname",
                table: "persons",
                columns: new[] { "last_name", "first_name", "middle_name" });

            migrationBuilder.CreateIndex(
                name: "ix_persons_rnokpp",
                table: "persons",
                column: "rnokpp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persons_status_kind_id",
                table: "persons",
                column: "status_kind_id");

            migrationBuilder.CreateIndex(
                name: "ux_persons_position_unit_id_not_null",
                table: "persons",
                column: "position_unit_id",
                unique: true,
                filter: "\"position_unit_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_plan_actions_move_type",
                table: "plan_actions",
                column: "move_type");

            migrationBuilder.CreateIndex(
                name: "ix_plan_actions_person_date",
                table: "plan_actions",
                columns: new[] { "person_id", "effective_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_position_units_code",
                table: "position_units",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_position_units_number",
                table: "position_units",
                column: "special_number");

            migrationBuilder.CreateIndex(
                name: "ix_position_units_short_name",
                table: "position_units",
                column: "short_name");

            migrationBuilder.CreateIndex(
                name: "ix_status_kinds_code",
                table: "status_kinds",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_status_kinds_name",
                table: "status_kinds",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_status_transitions_from_to",
                table: "status_transitions",
                columns: new[] { "from_status_kind_id", "to_status_kind_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_status_transitions_to_status_kind_id",
                table: "status_transitions",
                column: "to_status_kind_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person_position_assignments");

            migrationBuilder.DropTable(
                name: "person_statuses");

            migrationBuilder.DropTable(
                name: "plan_actions");

            migrationBuilder.DropTable(
                name: "status_transitions");

            migrationBuilder.DropTable(
                name: "persons");

            migrationBuilder.DropTable(
                name: "position_units");

            migrationBuilder.DropTable(
                name: "status_kinds");
        }
    }
}
