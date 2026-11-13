using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalElections.Data.Migrations
{
    /// <inheritdoc />
    public partial class MunicipalityAdminGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminRoleName",
                table: "Municipalities",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            // Carry existing data over to the new model: every municipality gets its own
            // admin group (Identity role), and the users that were linked to a municipality
            // through the MunicipalityAdmins join table become members of that group.
            migrationBuilder.Sql(
                "UPDATE Municipalities SET AdminRoleName = Name || ' Admin';");

            migrationBuilder.Sql(
                """
                INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                SELECT lower(hex(randomblob(16))), m.AdminRoleName, upper(m.AdminRoleName), lower(hex(randomblob(16)))
                FROM Municipalities m
                WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles r WHERE r.NormalizedName = upper(m.AdminRoleName));
                """);

            migrationBuilder.Sql(
                """
                INSERT OR IGNORE INTO AspNetUserRoles (UserId, RoleId)
                SELECT ma.ApplicationUserId, r.Id
                FROM MunicipalityAdmins ma
                JOIN Municipalities m ON m.MunicipalityId = ma.MunicipalityId
                JOIN AspNetRoles r ON r.Name = m.AdminRoleName;
                """);

            // The single shared "MunicipalityAdmin" role is replaced by the per-municipality groups.
            migrationBuilder.Sql(
                """
                DELETE FROM AspNetUserRoles WHERE RoleId IN (SELECT Id FROM AspNetRoles WHERE Name = 'MunicipalityAdmin');
                DELETE FROM AspNetRoles WHERE Name = 'MunicipalityAdmin';
                """);

            migrationBuilder.DropTable(
                name: "MunicipalityAdmins");

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_AdminRoleName",
                table: "Municipalities",
                column: "AdminRoleName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Municipalities_AdminRoleName",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "AdminRoleName",
                table: "Municipalities");

            migrationBuilder.CreateTable(
                name: "MunicipalityAdmins",
                columns: table => new
                {
                    ApplicationUserId = table.Column<string>(type: "TEXT", nullable: false),
                    MunicipalityId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalityAdmins", x => new { x.ApplicationUserId, x.MunicipalityId });
                    table.ForeignKey(
                        name: "FK_MunicipalityAdmins_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MunicipalityAdmins_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "MunicipalityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityAdmins_MunicipalityId",
                table: "MunicipalityAdmins",
                column: "MunicipalityId");
        }
    }
}
