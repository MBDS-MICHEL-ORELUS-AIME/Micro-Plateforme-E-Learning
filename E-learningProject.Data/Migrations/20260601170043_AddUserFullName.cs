using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_learningProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ADD COLUMN IF NOT EXISTS \"FullName\" character varying(200);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" DROP COLUMN IF EXISTS \"FullName\";");
        }
    }
}
