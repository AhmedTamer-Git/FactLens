using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Factlens.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceTypeToSearchRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "SearchRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "SearchRecords");
        }
    }
}
