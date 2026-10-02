using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToDo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "to_do_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    list_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    list_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_to_do_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "to_do_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_do_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_done = table.Column<bool>(type: "boolean", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_to_do_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_to_do_items_to_do_lists_to_do_list_id",
                        column: x => x.to_do_list_id,
                        principalTable: "to_do_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_to_do_items_to_do_list_id",
                table: "to_do_items",
                column: "to_do_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_to_do_lists_list_title",
                table: "to_do_lists",
                column: "list_title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "to_do_items");

            migrationBuilder.DropTable(
                name: "to_do_lists");
        }
    }
}
