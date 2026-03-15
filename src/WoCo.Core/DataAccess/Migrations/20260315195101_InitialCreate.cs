using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WoCo.Core.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Annotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Annotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Annotations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FloorplanRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FileContent = table.Column<byte[]>(type: "BLOB", nullable: false),
                    FileType = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    CoordinateSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    Origin = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<double>(type: "REAL", nullable: false),
                    Height = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorplanRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorplanRevisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnnotationRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnnotationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FloorplanRevisionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RawCoordinates = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedCoordinates = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnotationRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnotationRevisions_Annotations_AnnotationId",
                        column: x => x.AnnotationId,
                        principalTable: "Annotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnnotationRevisions_FloorplanRevisions_FloorplanRevisionId",
                        column: x => x.FloorplanRevisionId,
                        principalTable: "FloorplanRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationRevisions_AnnotationId_FloorplanRevisionId",
                table: "AnnotationRevisions",
                columns: new[] { "AnnotationId", "FloorplanRevisionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationRevisions_AnnotationId_RevisionNumber",
                table: "AnnotationRevisions",
                columns: new[] { "AnnotationId", "RevisionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationRevisions_FloorplanRevisionId_IsDeleted",
                table: "AnnotationRevisions",
                columns: new[] { "FloorplanRevisionId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_ProjectId",
                table: "Annotations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorplanRevisions_ProjectId_RevisionNumber",
                table: "FloorplanRevisions",
                columns: new[] { "ProjectId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedAtUtc",
                table: "Projects",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnotationRevisions");

            migrationBuilder.DropTable(
                name: "Annotations");

            migrationBuilder.DropTable(
                name: "FloorplanRevisions");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
