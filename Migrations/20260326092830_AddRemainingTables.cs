using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace chessPairingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Match",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WhitePlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BlackPlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WhiteResult = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BlackResult = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MatchDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ScheduledTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Match", x => x.GameId);
                    table.ForeignKey(
                        name: "FK_Match_AspNetUsers_BlackPlayerId",
                        column: x => x.BlackPlayerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Match_AspNetUsers_WhitePlayerId",
                        column: x => x.WhitePlayerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchQueue",
                columns: table => new
                {
                    QueueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TimeJoined = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ScheduledTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchQueue", x => x.QueueId);
                    table.ForeignKey(
                        name: "FK_MatchQueue_AspNetUsers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appeal",
                columns: table => new
                {
                    AppealId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdminResponse = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appeal", x => x.AppealId);
                    table.ForeignKey(
                        name: "FK_Appeal_AspNetUsers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appeal_Match_GameId",
                        column: x => x.GameId,
                        principalTable: "Match",
                        principalColumn: "GameId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appeal_GameId",
                table: "Appeal",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Appeal_PlayerId",
                table: "Appeal",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_BlackPlayerId",
                table: "Match",
                column: "BlackPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_WhitePlayerId",
                table: "Match",
                column: "WhitePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchQueue_PlayerId",
                table: "MatchQueue",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appeal");

            migrationBuilder.DropTable(
                name: "MatchQueue");

            migrationBuilder.DropTable(
                name: "Match");
        }
    }
}
