using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutfitPlaner_Applcation.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    User_Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Password_Hash = table.Column<string>(type: "TEXT", nullable: false),
                    Created_At = table.Column<DateTime>(type: "DATETIME", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "CapsuleWardrobe",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ID_User = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Created_At = table.Column<DateTime>(type: "DATETIME", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Is_Favorite = table.Column<bool>(type: "BOOLEAN", nullable: true, defaultValueSql: "FALSE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapsuleWardrobe", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CapsuleWardrobe_User_ID_User",
                        column: x => x.ID_User,
                        principalTable: "User",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clothing",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ID_User = table.Column<int>(type: "INTEGER", nullable: false),
                    Image_Url = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    Style = table.Column<string>(type: "TEXT", nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: false),
                    Season = table.Column<string>(type: "TEXT", nullable: false),
                    Condition = table.Column<int>(type: "INTEGER", nullable: true),
                    Added_at = table.Column<DateTime>(type: "DATETIME", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clothing", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Clothing_User_ID_User",
                        column: x => x.ID_User,
                        principalTable: "User",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThematicLook",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ID_User = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", nullable: false),
                    Created_At = table.Column<DateTime>(type: "DATETIME", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Is_Favorite = table.Column<bool>(type: "BOOLEAN", nullable: true, defaultValueSql: "FALSE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThematicLook", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ThematicLook_User_ID_User",
                        column: x => x.ID_User,
                        principalTable: "User",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClothingCapsule",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ID_Clothing = table.Column<int>(type: "INTEGER", nullable: false),
                    ID_CapsuleWardrobe = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClothingCapsule", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ClothingCapsule_CapsuleWardrobe_ID_CapsuleWardrobe",
                        column: x => x.ID_CapsuleWardrobe,
                        principalTable: "CapsuleWardrobe",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClothingCapsule_Clothing_ID_Clothing",
                        column: x => x.ID_Clothing,
                        principalTable: "Clothing",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClothingLook",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ID_Clothing = table.Column<int>(type: "INTEGER", nullable: false),
                    ID_Look = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClothingLook", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ClothingLook_Clothing_ID_Clothing",
                        column: x => x.ID_Clothing,
                        principalTable: "Clothing",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClothingLook_ThematicLook_ID_Look",
                        column: x => x.ID_Look,
                        principalTable: "ThematicLook",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapsuleWardrobe_ID_User",
                table: "CapsuleWardrobe",
                column: "ID_User");

            migrationBuilder.CreateIndex(
                name: "IX_Clothing_ID_User",
                table: "Clothing",
                column: "ID_User");

            migrationBuilder.CreateIndex(
                name: "IX_ClothingCapsule_ID_CapsuleWardrobe",
                table: "ClothingCapsule",
                column: "ID_CapsuleWardrobe");

            migrationBuilder.CreateIndex(
                name: "IX_ClothingCapsule_ID_Clothing",
                table: "ClothingCapsule",
                column: "ID_Clothing");

            migrationBuilder.CreateIndex(
                name: "IX_ClothingLook_ID_Clothing",
                table: "ClothingLook",
                column: "ID_Clothing");

            migrationBuilder.CreateIndex(
                name: "IX_ClothingLook_ID_Look",
                table: "ClothingLook",
                column: "ID_Look");

            migrationBuilder.CreateIndex(
                name: "IX_ThematicLook_ID_User",
                table: "ThematicLook",
                column: "ID_User");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_User_Name",
                table: "User",
                column: "User_Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClothingCapsule");

            migrationBuilder.DropTable(
                name: "ClothingLook");

            migrationBuilder.DropTable(
                name: "CapsuleWardrobe");

            migrationBuilder.DropTable(
                name: "Clothing");

            migrationBuilder.DropTable(
                name: "ThematicLook");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
