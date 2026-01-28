using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddMission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "missions");
        }
    }
}
