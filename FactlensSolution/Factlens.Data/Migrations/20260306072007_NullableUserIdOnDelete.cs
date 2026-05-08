using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Factlens.Data.Migrations
{
    /// <inheritdoc />
    public partial class NullableUserIdOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiRequestLogs_AspNetUsers_UserId",
                table: "AiRequestLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FeedbackRecords_AspNetUsers_UserId",
                table: "FeedbackRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchRecords_AspNetUsers_UserId",
                table: "SearchRecords");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "SearchRecords",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "FeedbackRecords",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AiRequestLogs",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_AiRequestLogs_AspNetUsers_UserId",
                table: "AiRequestLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FeedbackRecords_AspNetUsers_UserId",
                table: "FeedbackRecords",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SearchRecords_AspNetUsers_UserId",
                table: "SearchRecords",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiRequestLogs_AspNetUsers_UserId",
                table: "AiRequestLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FeedbackRecords_AspNetUsers_UserId",
                table: "FeedbackRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchRecords_AspNetUsers_UserId",
                table: "SearchRecords");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "SearchRecords",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "FeedbackRecords",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AiRequestLogs",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AiRequestLogs_AspNetUsers_UserId",
                table: "AiRequestLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FeedbackRecords_AspNetUsers_UserId",
                table: "FeedbackRecords",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchRecords_AspNetUsers_UserId",
                table: "SearchRecords",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
