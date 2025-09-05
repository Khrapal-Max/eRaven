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
                name: "plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    planned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    time_kind = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    group_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    tool_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    task_description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, defaultValue: "system"),
                    recorded_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.id);
                    table.CheckConstraint("ck_plans_plan_number_not_blank", "char_length(trim(plan_number)) > 0");
                    table.CheckConstraint("ck_plans_planned_at_quarter", "(EXTRACT(MINUTE FROM planned_at_utc)::int % 15 = 0) AND EXTRACT(SECOND FROM planned_at_utc) = 0");
                });

            migrationBuilder.CreateTable(
                name: "position_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    short_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    org_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
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
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_kinds", x => x.id);
                    table.CheckConstraint("ck_status_kinds_code_not_blank", "char_length(trim(code)) > 0");
                    table.CheckConstraint("ck_status_kinds_name_not_blank", "char_length(trim(name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    effective_moment_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, defaultValue: "system"),
                    recorded_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("ck_orders_effective_moment_quarter", "(EXTRACT(MINUTE FROM effective_moment_utc)::int % 15 = 0) AND EXTRACT(SECOND FROM effective_moment_utc) = 0");
                    table.CheckConstraint("ck_orders_name_not_blank", "char_length(trim(name)) > 0");
                    table.ForeignKey(
                        name: "FK_orders_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_participant_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    rank = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    position_snapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    weapon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    callsign = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status_kind_id = table.Column<int>(type: "integer", nullable: true),
                    status_kind_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, defaultValue: "system"),
                    recorded_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    plan_id1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_participant_snapshots", x => x.id);
                    table.CheckConstraint("ck_pps_full_name_not_blank", "char_length(trim(full_name)) > 0");
                    table.ForeignKey(
                        name: "FK_plan_participant_snapshots_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rnokpp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    rank = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    bzvp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    weapon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    callsign = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    position_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_kind_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
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
                        onDelete: ReferentialAction.Restrict);
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
                    table.CheckConstraint("ck_status_transitions_from_ne_to", "\"from_status_kind_id\" <> \"to_status_kind_id\"");
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
                    from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    to_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, defaultValue: "system"),
                    modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_position_assignments", x => x.id);
                    table.CheckConstraint("ck_person_position_assignments_dates", "(\"to_utc\" IS NULL) OR (\"to_utc\" > \"from_utc\")");
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
                    from_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    to_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    author = table.Column<string>(type: "text", nullable: true, defaultValue: "system"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_statuses", x => x.id);
                    table.CheckConstraint("ck_person_status_dates", "(\"to_date\" IS NULL) OR (\"to_date\" > \"from_date\")");
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

            migrationBuilder.InsertData(
                table: "status_kinds",
                columns: new[] { "id", "author", "code", "is_active", "modified", "name", "order" },
                values: new object[,]
                {
                    { 1, "system", "30", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "В районі", 30 },
                    { 2, "system", "100", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "В БР", 10 },
                    { 3, "system", "100", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "В БТГр", 20 },
                    { 4, "system", "нб", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Переміщення", 40 },
                    { 5, "system", "нб", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Звільнення", 50 },
                    { 6, "system", "РОЗПОР", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Розпорядження", 60 },
                    { 7, "system", "ВДР", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Відрядження", 70 },
                    { 8, "system", "ВДР", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Фахове навчання", 80 },
                    { 9, "system", "В", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Відпустка", 90 },
                    { 10, "system", "Л_Х", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Проходження ВЛК", 100 },
                    { 11, "system", "Л_Х", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Направлення на МСЕК", 110 },
                    { 12, "system", "Л_Х", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Лікування по хворобі", 120 },
                    { 13, "system", "Л_Б", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Лікування по пораненню", 130 },
                    { 14, "system", "БВ", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Безвісті", 140 },
                    { 15, "system", "П", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Полон", 150 },
                    { 16, "system", "200", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Загибель", 160 },
                    { 17, "system", "А", true, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Арешт", 170 },
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
                    { 25, 2, 1 },
                    { 26, 2, 12 },
                    { 27, 2, 13 },
                    { 28, 2, 14 },
                    { 29, 2, 15 },
                    { 30, 2, 16 },
                    { 31, 2, 18 },
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
                name: "ix_orders_effective_moment_utc",
                table: "orders",
                column: "effective_moment_utc");

            migrationBuilder.CreateIndex(
                name: "ix_orders_name",
                table: "orders",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_orders_recorded_utc",
                table: "orders",
                column: "recorded_utc");

            migrationBuilder.CreateIndex(
                name: "ux_orders_plan_id",
                table: "orders",
                column: "plan_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ppassign_person_from",
                table: "person_position_assignments",
                columns: new[] { "person_id", "from_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_ppassign_person_to",
                table: "person_position_assignments",
                columns: new[] { "person_id", "to_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_ppassign_position_from",
                table: "person_position_assignments",
                columns: new[] { "position_unit_id", "from_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_ppassign_person_open",
                table: "person_position_assignments",
                column: "person_id",
                unique: true,
                filter: "\"to_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ppassign_position_open",
                table: "person_position_assignments",
                column: "position_unit_id",
                unique: true,
                filter: "\"to_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_statuses_active_unique_per_person",
                table: "person_statuses",
                column: "person_id",
                unique: true,
                filter: "\"to_date\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_statuses_person_from",
                table: "person_statuses",
                columns: new[] { "person_id", "from_date" });

            migrationBuilder.CreateIndex(
                name: "ix_person_statuses_person_to",
                table: "person_statuses",
                columns: new[] { "person_id", "to_date" });

            migrationBuilder.CreateIndex(
                name: "IX_person_statuses_status_kind_id",
                table: "person_statuses",
                column: "status_kind_id");

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
                name: "ix_pps_person_id",
                table: "plan_participant_snapshots",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_pps_plan_id",
                table: "plan_participant_snapshots",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_pps_plan_person",
                table: "plan_participant_snapshots",
                columns: new[] { "plan_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_pps_recorded_utc",
                table: "plan_participant_snapshots",
                column: "recorded_utc");

            migrationBuilder.CreateIndex(
                name: "ix_plans_planned_at_utc",
                table: "plans",
                column: "planned_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_plans_state_planned",
                table: "plans",
                columns: new[] { "state", "planned_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_plans_type_planned",
                table: "plans",
                columns: new[] { "type", "planned_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_plans_plan_number",
                table: "plans",
                column: "plan_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_position_units_code",
                table: "position_units",
                column: "code");

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
                name: "orders");

            migrationBuilder.DropTable(
                name: "person_position_assignments");

            migrationBuilder.DropTable(
                name: "person_statuses");

            migrationBuilder.DropTable(
                name: "plan_participant_snapshots");

            migrationBuilder.DropTable(
                name: "status_transitions");

            migrationBuilder.DropTable(
                name: "persons");

            migrationBuilder.DropTable(
                name: "plans");

            migrationBuilder.DropTable(
                name: "position_units");

            migrationBuilder.DropTable(
                name: "status_kinds");
        }
    }
}
