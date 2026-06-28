using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationApplicantUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicantUserId",
                table: "Applications",
                column: "ApplicantUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_User_ApplicantUserId",
                table: "Applications",
                column: "ApplicantUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_User_ApplicantUserId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_ApplicantUserId",
                table: "Applications");
        }
    }
}
