using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace transferFiles.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_MeaxDB_it : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "it");

            migrationBuilder.CreateTable(
                name: "transferfiles_links",
                schema: "it",
                columns: table => new
                {
                    link_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_by_login = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_by_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    created_by_department = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    link_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    link_password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferfiles_links", x => x.link_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transferfiles_links_created_by_login_created_at",
                schema: "it",
                table: "transferfiles_links",
                columns: new[] { "created_by_login", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transferfiles_links",
                schema: "it");
        }
    }
}
