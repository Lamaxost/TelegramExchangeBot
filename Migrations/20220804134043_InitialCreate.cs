using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramEchangeNew.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BanList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExchgangingCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageServerDublicatesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserChatId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedVideos_Users_UserChatId",
                        column: x => x.UserChatId,
                        principalTable: "Users",
                        principalColumn: "ChatId");
                });

            migrationBuilder.CreateTable(
                name: "Sended",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserChatId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sended", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sended_Users_UserChatId",
                        column: x => x.UserChatId,
                        principalTable: "Users",
                        principalColumn: "ChatId");
                });

            migrationBuilder.CreateTable(
                name: "VideoProducts",
                columns: table => new
                {
                    UniqId = table.Column<string>(type: "TEXT", nullable: false),
                    StorageMessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    StorageId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserChatId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoProducts", x => x.UniqId);
                    table.ForeignKey(
                        name: "FK_VideoProducts_Users_UserChatId",
                        column: x => x.UserChatId,
                        principalTable: "Users",
                        principalColumn: "ChatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedVideos_UserChatId",
                table: "ReceivedVideos",
                column: "UserChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Sended_UserChatId",
                table: "Sended",
                column: "UserChatId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProducts_UserChatId",
                table: "VideoProducts",
                column: "UserChatId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanList");

            migrationBuilder.DropTable(
                name: "ReceivedVideos");

            migrationBuilder.DropTable(
                name: "Sended");

            migrationBuilder.DropTable(
                name: "VideoProducts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
