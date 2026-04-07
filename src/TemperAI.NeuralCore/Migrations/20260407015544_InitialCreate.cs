using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemperAI.NeuralCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Project = table.Column<string>(type: "varchar", maxLength: 200, nullable: false),
                    Directory = table.Column<string>(type: "nvarchar", maxLength: 500, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "varchar", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Observations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "varchar", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar", maxLength: 4000, nullable: false),
                    Project = table.Column<string>(type: "varchar", maxLength: 200, nullable: false),
                    TopicKey = table.Column<string>(type: "varchar", maxLength: 200, nullable: true),
                    RevisionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observations_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Observations_Project",
                table: "Observations",
                column: "Project");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_SessionId",
                table: "Observations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_TopicKey",
                table: "Observations",
                column: "TopicKey");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Project",
                table: "Sessions",
                column: "Project");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Observations");

            migrationBuilder.DropTable(
                name: "Sessions");
        }
    }
}
