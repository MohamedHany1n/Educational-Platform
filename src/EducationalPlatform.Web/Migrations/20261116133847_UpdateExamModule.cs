using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlatform.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExamModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_ExamId",
                table: "ExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_AttemptAnswers_AttemptId",
                table: "AttemptAnswers");

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "ExamAttempts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
            WITH NumberedAttempts AS
            (
            SELECT Id,
               ROW_NUMBER() OVER (
                   PARTITION BY ExamId, StudentId
                   ORDER BY StartedAt, Id
               ) AS NewAttemptNumber
            FROM ExamAttempts
            )
            UPDATE ExamAttempts
            SET AttemptNumber = NumberedAttempts.NewAttemptNumber
            FROM ExamAttempts
            INNER JOIN NumberedAttempts
            ON ExamAttempts.Id = NumberedAttempts.Id;
            ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_Marks",
                table: "Questions",
                sql: "[Marks] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_Type",
                table: "Questions",
                sql: "[Type] IN ('MCQ', 'Essay')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Exams_DurationMinutes",
                table: "Exams",
                sql: "[DurationMinutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Exams_MaxAttempts",
                table: "Exams",
                sql: "[MaxAttempts] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ExamId_StudentId_AttemptNumber",
                table: "ExamAttempts",
                columns: new[] { "ExamId", "StudentId", "AttemptNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExamAttempts_AttemptNumber",
                table: "ExamAttempts",
                sql: "[AttemptNumber] >= 1");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_AttemptId_QuestionId",
                table: "AttemptAnswers",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_Marks",
                table: "Questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_Type",
                table: "Questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Exams_DurationMinutes",
                table: "Exams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Exams_MaxAttempts",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_ExamId_StudentId_AttemptNumber",
                table: "ExamAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExamAttempts_AttemptNumber",
                table: "ExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_AttemptAnswers_AttemptId_QuestionId",
                table: "AttemptAnswers");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "ExamAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ExamId",
                table: "ExamAttempts",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_AttemptId",
                table: "AttemptAnswers",
                column: "AttemptId");
        }
    }
}
