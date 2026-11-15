using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlatform.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPhonesTransactionAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenderPhoneNumber",
                table: "PaymentRequests",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionNumber",
                table: "PaymentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LinkedAt",
                table: "ParentStudents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "LinkedByAdminId",
                table: "ParentStudents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ParentPhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_PaymentMethod_TransactionNumber",
                table: "PaymentRequests",
                columns: new[] { "PaymentMethod", "TransactionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentStudents_LinkedByAdminId",
                table: "ParentStudents",
                column: "LinkedByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParentStudents_AspNetUsers_LinkedByAdminId",
                table: "ParentStudents",
                column: "LinkedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParentStudents_AspNetUsers_LinkedByAdminId",
                table: "ParentStudents");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_PaymentMethod_TransactionNumber",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_ParentStudents_LinkedByAdminId",
                table: "ParentStudents");

            migrationBuilder.DropColumn(
                name: "SenderPhoneNumber",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "TransactionNumber",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "LinkedAt",
                table: "ParentStudents");

            migrationBuilder.DropColumn(
                name: "LinkedByAdminId",
                table: "ParentStudents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ParentPhoneNumber",
                table: "AspNetUsers");
        }
    }
}
