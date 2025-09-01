using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Moviequest_proj.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchListMovie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WatchLists_Movies_MovieID",
                table: "WatchLists");

            migrationBuilder.AlterColumn<int>(
                name: "MovieID",
                table: "WatchLists",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "WatchListMovies",
                columns: table => new
                {
                    WatchListID = table.Column<int>(type: "int", nullable: false),
                    MovieID = table.Column<int>(type: "int", nullable: false),
                    WatchListMovieID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchListMovies", x => new { x.WatchListID, x.MovieID });
                    table.ForeignKey(
                        name: "FK_WatchListMovies_Movies_MovieID",
                        column: x => x.MovieID,
                        principalTable: "Movies",
                        principalColumn: "MovieID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WatchListMovies_WatchLists_WatchListID",
                        column: x => x.WatchListID,
                        principalTable: "WatchLists",
                        principalColumn: "WatchListID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WatchListMovies_MovieID",
                table: "WatchListMovies",
                column: "MovieID");

            migrationBuilder.AddForeignKey(
                name: "FK_WatchLists_Movies_MovieID",
                table: "WatchLists",
                column: "MovieID",
                principalTable: "Movies",
                principalColumn: "MovieID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WatchLists_Movies_MovieID",
                table: "WatchLists");

            migrationBuilder.DropTable(
                name: "WatchListMovies");

            migrationBuilder.AlterColumn<int>(
                name: "MovieID",
                table: "WatchLists",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WatchLists_Movies_MovieID",
                table: "WatchLists",
                column: "MovieID",
                principalTable: "Movies",
                principalColumn: "MovieID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
