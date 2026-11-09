using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admission.DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admission_holds",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    class_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    student_id = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admission_holds", x => x.id);
                    table.CheckConstraint("ck_admission_holds_student_id_17_digits", "\"student_id\" ~ '^[0-9]{17}$'");
                    table.ForeignKey(
                        name: "FK_admission_holds_admission_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "admission_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admission_holds_class_status_expiry",
                table: "admission_holds",
                columns: new[] { "class_id", "status", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_holds_student_id",
                table: "admission_holds",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ux_admission_holds_class_student_active",
                table: "admission_holds",
                columns: new[] { "class_id", "student_id" },
                unique: true,
                filter: "\"status\" IN ('Held', 'Confirmed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admission_holds");
        }
    }
}
