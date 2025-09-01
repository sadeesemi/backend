using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Moviequest_proj.Models;

namespace Moviequest_proj.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Genre> Genres { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<WatchList> WatchLists { get; set; }
        public DbSet<WatchListMovie> WatchListMovies { get; set; }
        public DbSet<WatchedMovie> WatchedMovies { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // MovieGenre many-to-many
            builder.Entity<MovieGenre>()
                .HasKey(mg => new { mg.MovieID, mg.GenreID });

            builder.Entity<MovieGenre>()
                .HasOne(mg => mg.Movie)
                .WithMany(m => m.MovieGenres)
                .HasForeignKey(mg => mg.MovieID);

            builder.Entity<MovieGenre>()
                .HasOne(mg => mg.Genre)
                .WithMany(g => g.MovieGenres)
                .HasForeignKey(mg => mg.GenreID);

            builder.Entity<WatchListMovie>()
    .HasKey(wlm => new { wlm.WatchListID, wlm.MovieID });

            builder.Entity<WatchListMovie>()
                .HasOne(wlm => wlm.WatchList)
                .WithMany(wl => wl.WatchListMovies)
                .HasForeignKey(wlm => wlm.WatchListID);

            builder.Entity<WatchListMovie>()
                .HasOne(wlm => wlm.Movie)
                .WithMany(m => m.WatchListMovies) // ✅ navigation
                .HasForeignKey(wlm => wlm.MovieID);
            ;
        }
    }
}
