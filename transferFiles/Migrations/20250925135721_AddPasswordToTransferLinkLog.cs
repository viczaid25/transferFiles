using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace transferFiles.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordToTransferLinkLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "TransferLinkLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "TransferLinkLogs");
        }
    }
}
