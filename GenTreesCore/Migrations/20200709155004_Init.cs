using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace GenTreesCore.Migrations
{
    public partial class Init : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Login = table.Column<string>(maxLength: 255),
                    PasswordHash = table.Column<string>(maxLength: 128),
                    Salt = table.Column<string>(maxLength: 16),
                    Email = table.Column<string>(maxLength: 255),
                    EmailConfirmed = table.Column<bool>(),
                    DateCreated = table.Column<DateTime>(),
                    LastVisit = table.Column<DateTime>(),
                    Role = table.Column<string>(maxLength: 32)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", t => t.Id);
                });

            migrationBuilder.CreateTable(
               name: "GenTreeDateTimeSettings",
               columns: table => new
               {
                   Id = table.Column<int>()
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                   Name = table.Column<string>(maxLength: 255),
                   YearMonthCount = table.Column<int>(),
                   OwnerId = table.Column<int>(),
                   IsPrivate = table.Column<bool>()
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_GenTreeDateTimeSettings", t => t.Id);
                   table.ForeignKey("FK_GenTreeDateTimeSettings_Users", t => t.OwnerId, "Users","Id", onDelete: ReferentialAction.Cascade);
               });

            migrationBuilder.CreateTable(
               name: "GenTreeEras",
               columns: table => new
               {
                   Id = table.Column<int>()
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                   GenTreeDateTimeSettingId = table.Column<int>(),
                   Name = table.Column<string>(maxLength: 255),
                   ShortName = table.Column<string>(maxLength: 32),
                   Description = table.Column<string>(nullable : true, maxLength: 1024),
                   ThroughBeginYear = table.Column<int>(),
                   YearCount = table.Column<int>()
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_GenTreeEras", t => t.Id);
                   table.ForeignKey("FK_GenTreeEras_GenTreeDateTimeSettings", t => t.GenTreeDateTimeSettingId, "GenTreeDateTimeSettings", "Id", onDelete: ReferentialAction.Cascade);
               });

            migrationBuilder.CreateTable(
               name: "GenTreeDates",
               columns: table => new
               {
                   Id = table.Column<int>()
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                   EraId = table.Column<int>(),
                   Year = table.Column<int>(),
                   Month = table.Column<int>(),
                   Day = table.Column<int>(),
                   Hour = table.Column<int>(),
                   Minute = table.Column<int>(),
                   Second = table.Column<int>()
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_GenTreeDates", t => t.Id);
                   table.ForeignKey("FK_GenTreeDates_GenTreeEras", t => t.EraId, "GenTreeEras", "Id", onDelete: ReferentialAction.Cascade);
               });

            migrationBuilder.CreateTable(
                name: "GenTrees",
                columns: table => new
                {
                    Id = table.Column<int>()
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 255),
                    Description = table.Column<string>(nullable: true, maxLength: 4096),
                    GenTreeDateTimeSettingId = table.Column<int>(),
                    DateCreated = table.Column<DateTime>(),
                    LastUpdated = table.Column<DateTime>(),
                    OwnerId = table.Column<int>(),
                    IsPrivate = table.Column<bool>(),
                    Image = table.Column<string>(maxLength: 255, nullable: true)

                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenTrees", t => t.Id);
                    table.ForeignKey("FK_GenTrees_DateTimeSettings", t => t.GenTreeDateTimeSettingId, "GenTreeDateTimeSettings", "Id");
                    table.ForeignKey("FK_GenTrees_Users", t => t.OwnerId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
               name: "Persons",
               columns: table => new
               {
                   Id = table.Column<int>()
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                   GenTreeId = table.Column<int>(),
                   LastName = table.Column<string>(maxLength: 255),
                   FirstName = table.Column<string>(maxLength: 255),
                   MiddleName = table.Column<string>(nullable: true, maxLength: 255),
                   Gender = table.Column<string>(nullable: true, maxLength: 255),
                   BirthDateId = table.Column<int>(nullable: true),
                   DeathDateId = table.Column<int>(nullable: true),
                   BirthPlace = table.Column<string>(nullable: true, maxLength: 255),
                   Biography = table.Column<string>(nullable: true, maxLength: 4096),
                   Image = table.Column<string>(nullable: true, maxLength: 255)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_Persons", t => t.Id);
                   table.ForeignKey("FK_Persons_GenTrees", t => t.GenTreeId, "GenTrees", "Id");
                   table.ForeignKey("FK_Persons_GenTreeDateTime_BirthDate", t => t.BirthDateId, "GenTreeDates", "Id");
                   table.ForeignKey("FK_Persons_GenTreeDateTime_DeathDate", t => t.DeathDateId, "GenTreeDates", "Id");
               });

            migrationBuilder.CreateTable(
              name: "CustomPersonDescriptionTemplates",
              columns: table => new
              {
                  Id = table.Column<int>()
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                  GenTreeId = table.Column<int>(),
                  Name = table.Column<string>(maxLength: 255),
                  Type = table.Column<string>(maxLength: 64)
              },
              constraints: table =>
              {
                  table.PrimaryKey("PK_CustomPersonsDescriptionTemplates", t => t.Id);
                  table.ForeignKey("FK_CustomPersonsDescriptionTemplates_GenTree", t => t.GenTreeId, "GenTrees", "Id", onDelete: ReferentialAction.Cascade);
              });

            migrationBuilder.CreateTable(
              name: "CustomPersonDescriptions",
              columns: table => new
              {
                  Id = table.Column<int>()
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                  PersonId = table.Column<int>(),
                  TemplateId = table.Column<int>(),
                  Value = table.Column<string>(maxLength: 512)
              },
              constraints: table =>
              {
                  table.PrimaryKey("PK_CustomPersonsDescriptions", t => t.Id);
                  table.ForeignKey("FK_CustomPersonsDescriptions_Templates", t => t.TemplateId, "CustomPersonDescriptionTemplates", "Id", onDelete: ReferentialAction.Cascade);
                  table.ForeignKey("FK_CustomPersonsDescriptions_Persons", t => t.PersonId, "Persons", "Id", onDelete: ReferentialAction.Cascade);
              });

            migrationBuilder.CreateTable(
               name: "Relations",
               columns: table => new
               {
                   Id = table.Column<int>()
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                   SourcePersonId = table.Column<int>(),
                   TargetPersonId = table.Column<int>(),
                   RelationType = table.Column<string>(maxLength: 64),
                   IsFinished = table.Column<bool>(nullable: true),
                   SecondParentId = table.Column<int>(nullable: true),
                   RelationRate = table.Column<string>(nullable: true)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_Relations", t => t.Id);
                   table.ForeignKey("FK_Relations_Persons_SourcePerson", t => t.SourcePersonId, "Persons", "Id");
                   table.ForeignKey("FK_Relations_Persons_TargetPerson", t => t.TargetPersonId, "Persons", "Id");
                   table.ForeignKey("FK_Relations_Persons_SecondParent", t => t.SecondParentId, "Persons", "Id");
               });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("CustomPersonDescriptions");
            migrationBuilder.DropTable("CustomPersonDescriptionTemplates");      
            migrationBuilder.DropTable("Relations");
            migrationBuilder.DropTable("Persons");
            migrationBuilder.DropTable("GenTrees");

            migrationBuilder.DropTable("GenTreeDates");
            migrationBuilder.DropTable("GenTreeEras");
            migrationBuilder.DropTable("GenTreeDateTimeSettings");

            migrationBuilder.DropTable("Users");
        }
    }
}