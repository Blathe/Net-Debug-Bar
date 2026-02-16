using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NetDebugBar.Demo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Games",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PublisherId",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Games",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseDate",
                table: "Games",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Publishers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FoundedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publishers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameGenre",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "int", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameGenre", x => new { x.GameId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_GameGenre_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameGenre_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Genres",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Fast-paced games focused on physical challenges", "Action" },
                    { 2, "Role-playing games with character development", "RPG" },
                    { 3, "Games requiring careful planning and tactics", "Strategy" },
                    { 4, "Story-driven exploration games", "Adventure" },
                    { 5, "Games focused on problem-solving", "Puzzle" },
                    { 6, "Athletic competition simulations", "Sports" },
                    { 7, "Combat games with ranged weapons", "Shooter" }
                });

            migrationBuilder.InsertData(
                table: "Publishers",
                columns: new[] { "Id", "Country", "FoundedDate", "Name", "Website" },
                values: new object[,]
                {
                    { 1, "USA", new DateTime(1982, 5, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Electronic Arts", "https://ea.com" },
                    { 2, "France", new DateTime(1986, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ubisoft", "https://ubisoft.com" },
                    { 3, "Japan", new DateTime(1889, 9, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nintendo", "https://nintendo.com" },
                    { 4, "USA", new DateTime(1996, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "Valve Corporation", "https://valvesoftware.com" },
                    { 5, "Poland", new DateTime(1994, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "CD Projekt", "https://cdprojekt.com" }
                });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "CoverImageUrl", "Description", "Name", "Price", "PublisherId", "Rating", "ReleaseDate" },
                values: new object[,]
                {
                    { 1, "https://placeholder.com/portal2.jpg", "A mind-bending puzzle platformer with portal mechanics", "Portal 2", 19.99m, 4, 4.9000000000000004, new DateTime(2011, 4, 19, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "https://placeholder.com/witcher3.jpg", "Epic open-world RPG following Geralt of Rivia", "The Witcher 3", 39.99m, 5, 4.7999999999999998, new DateTime(2015, 5, 19, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, "https://placeholder.com/zelda.jpg", "Revolutionary open-world adventure in Hyrule", "Zelda: Breath of the Wild", 59.99m, 3, 4.9000000000000004, new DateTime(2017, 3, 3, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, "https://placeholder.com/acvalhalla.jpg", "Viking saga in medieval England", "Assassin's Creed Valhalla", 59.99m, 2, 4.2000000000000002, new DateTime(2020, 11, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, "https://placeholder.com/fifa24.jpg", "The ultimate football simulation", "FIFA 24", 69.99m, 1, 3.7999999999999998, new DateTime(2023, 9, 29, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "GameGenre",
                columns: new[] { "GameId", "GenreId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 5 },
                    { 2, 1 },
                    { 2, 2 },
                    { 2, 4 },
                    { 3, 1 },
                    { 3, 2 },
                    { 3, 4 },
                    { 4, 1 },
                    { 4, 2 },
                    { 4, 4 },
                    { 5, 6 }
                });

            migrationBuilder.InsertData(
                table: "Reviews",
                columns: new[] { "Id", "Comment", "GameId", "Rating", "ReviewDate", "ReviewerName" },
                values: new object[,]
                {
                    { 1, "Best puzzle game ever!", 1, 5, new DateTime(2024, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "John Doe" },
                    { 2, "Absolutely brilliant mechanics", 1, 5, new DateTime(2024, 2, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Jane Smith" },
                    { 3, "Masterpiece of storytelling", 2, 5, new DateTime(2024, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Alex Johnson" },
                    { 4, "Great game, minor bugs", 2, 4, new DateTime(2024, 2, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sarah Williams" },
                    { 5, "Changed open-world gaming forever", 3, 5, new DateTime(2024, 1, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mike Brown" },
                    { 6, "Perfect adventure", 3, 5, new DateTime(2024, 2, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "Emily Davis" },
                    { 7, "Good Viking experience", 4, 4, new DateTime(2024, 1, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Chris Wilson" },
                    { 8, "Same as last year", 5, 3, new DateTime(2024, 2, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pat Martinez" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_PublisherId",
                table: "Games",
                column: "PublisherId");

            migrationBuilder.CreateIndex(
                name: "IX_GameGenre_GenreId",
                table: "GameGenre",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_GameId",
                table: "Reviews",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Publishers_PublisherId",
                table: "Games",
                column: "PublisherId",
                principalTable: "Publishers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Publishers_PublisherId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "GameGenre");

            migrationBuilder.DropTable(
                name: "Publishers");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_Games_PublisherId",
                table: "Games");

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PublisherId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "Games");
        }
    }
}
