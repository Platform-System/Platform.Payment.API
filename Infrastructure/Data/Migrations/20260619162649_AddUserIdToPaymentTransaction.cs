using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Payment.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToPaymentTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Payments");
        }
    }
}
