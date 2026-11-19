using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_ExternarReference",
                table: "ServiceRequests");

            migrationBuilder.RenameColumn(
                name: "ExternarReference",
                table: "ServiceRequests",
                newName: "ExternalReference");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "ServiceRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ServiceRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "ServiceRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedAt",
                table: "ServiceRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ServiceRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // xmin es una columna de sistema de Postgres (token de concurrencia): no se crea.

            // Filas creadas antes de esta migración.
            migrationBuilder.Sql("UPDATE \"ServiceRequests\" SET \"CreatedAt\" = now(), \"UpdatedAt\" = now();");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ExternalReference",
                table: "ServiceRequests",
                column: "ExternalReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Status_CreatedAt",
                table: "ServiceRequests",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_ExternalReference",
                table: "ServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_Status_CreatedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ServiceRequests");


            migrationBuilder.RenameColumn(
                name: "ExternalReference",
                table: "ServiceRequests",
                newName: "ExternarReference");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ExternarReference",
                table: "ServiceRequests",
                column: "ExternarReference");
        }
    }
}
