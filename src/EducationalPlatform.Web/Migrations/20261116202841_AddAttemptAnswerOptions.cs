using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlatform.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptAnswerOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttemptAnswerOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptAnswerId = table.Column<int>(type: "int", nullable: false),
                    QuestionOptionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptAnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttemptAnswerOptions_AttemptAnswers_AttemptAnswerId",
                        column: x => x.AttemptAnswerId,
                        principalTable: "AttemptAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttemptAnswerOptions_QuestionOptions_QuestionOptionId",
                        column: x => x.QuestionOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswerOptions_AttemptAnswerId_QuestionOptionId",
                table: "AttemptAnswerOptions",
                columns: new[] { "AttemptAnswerId", "QuestionOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswerOptions_QuestionOptionId",
                table: "AttemptAnswerOptions",
                column: "QuestionOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttemptAnswerOptions");
        }
    }
}
