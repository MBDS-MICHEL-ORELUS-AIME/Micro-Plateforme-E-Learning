using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_learningProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkUserForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentBadges_StudentId_BadgeName",
                table: "StudentBadges");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "StudentBadges");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "LessonProgressions");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "DiscussionThreads");

            migrationBuilder.DropColumn(
                name: "ReporterStudentId",
                table: "DiscussionReports");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Certificates");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "StudentBadges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Quizzes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "QuizResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "LessonProgressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Enrollments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuthorId",
                table: "DiscussionThreads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModuleId",
                table: "DiscussionThreads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReporterId",
                table: "DiscussionReports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "DiscussionReplies");

            migrationBuilder.AddColumn<int>(
                name: "AuthorId",
                table: "DiscussionReplies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Certificates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Purge des données demo dont le UserId/AuthorId = 0 ne référence aucun User.
            // Les données seront re-seed automatiquement au démarrage de l'application.
            migrationBuilder.Sql(@"DELETE FROM ""DiscussionReplies"";");
            migrationBuilder.Sql(@"DELETE FROM ""DiscussionReports"";");
            migrationBuilder.Sql(@"DELETE FROM ""DiscussionThreads"";");
            migrationBuilder.Sql(@"DELETE FROM ""StudentBadges"";");
            migrationBuilder.Sql(@"DELETE FROM ""QuizResults"";");
            migrationBuilder.Sql(@"DELETE FROM ""LessonProgressions"";");
            migrationBuilder.Sql(@"DELETE FROM ""Enrollments"";");
            migrationBuilder.Sql(@"DELETE FROM ""Certificates"";");

            migrationBuilder.CreateIndex(
                name: "IX_StudentBadges_UserId_BadgeName",
                table: "StudentBadges",
                columns: new[] { "UserId", "BadgeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_CreatedByUserId",
                table: "Quizzes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_UserId",
                table: "QuizResults",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgressions_UserId_LessonId",
                table: "LessonProgressions",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserId_ModuleId",
                table: "Enrollments",
                columns: new[] { "UserId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionThreads_AuthorId",
                table: "DiscussionThreads",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionThreads_ModuleId",
                table: "DiscussionThreads",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReports_ReporterId",
                table: "DiscussionReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionReplies_AuthorId",
                table: "DiscussionReplies",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_UserId",
                table: "Certificates",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Users_UserId",
                table: "Certificates",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionReplies_Users_AuthorId",
                table: "DiscussionReplies",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionReports_Users_ReporterId",
                table: "DiscussionReports",
                column: "ReporterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionThreads_Modules_ModuleId",
                table: "DiscussionThreads",
                column: "ModuleId",
                principalTable: "Modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionThreads_Users_AuthorId",
                table: "DiscussionThreads",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Users_UserId",
                table: "Enrollments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonProgressions_Users_UserId",
                table: "LessonProgressions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizResults_Users_UserId",
                table: "QuizResults",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Users_CreatedByUserId",
                table: "Quizzes",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentBadges_Users_UserId",
                table: "StudentBadges",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Users_UserId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionReplies_Users_AuthorId",
                table: "DiscussionReplies");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionReports_Users_ReporterId",
                table: "DiscussionReports");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionThreads_Modules_ModuleId",
                table: "DiscussionThreads");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionThreads_Users_AuthorId",
                table: "DiscussionThreads");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Users_UserId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonProgressions_Users_UserId",
                table: "LessonProgressions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizResults_Users_UserId",
                table: "QuizResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Users_CreatedByUserId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentBadges_Users_UserId",
                table: "StudentBadges");

            migrationBuilder.DropIndex(
                name: "IX_StudentBadges_UserId_BadgeName",
                table: "StudentBadges");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_CreatedByUserId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_QuizResults_UserId",
                table: "QuizResults");

            migrationBuilder.DropIndex(
                name: "IX_LessonProgressions_UserId_LessonId",
                table: "LessonProgressions");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_UserId_ModuleId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionThreads_AuthorId",
                table: "DiscussionThreads");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionThreads_ModuleId",
                table: "DiscussionThreads");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionReports_ReporterId",
                table: "DiscussionReports");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionReplies_AuthorId",
                table: "DiscussionReplies");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_UserId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "StudentBadges");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LessonProgressions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "DiscussionThreads");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "DiscussionThreads");

            migrationBuilder.DropColumn(
                name: "ReporterId",
                table: "DiscussionReports");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Certificates");

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "StudentBadges",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "QuizResults",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "LessonProgressions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "Enrollments",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "DiscussionThreads",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReporterStudentId",
                table: "DiscussionReports",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "DiscussionReplies");

            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "DiscussionReplies",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "Certificates",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StudentBadges_StudentId_BadgeName",
                table: "StudentBadges",
                columns: new[] { "StudentId", "BadgeName" },
                unique: true);
        }
    }
}
