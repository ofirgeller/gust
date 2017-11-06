using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace GustEfcConsumer.Model
{
    public class BloggerContext : DbContext
    {
        static readonly LoggerFactory MyLoggerFactory = new LoggerFactory(new[] { new ConsoleLoggerProvider((_, __) => true, true) });

        static string pgsqlConnString = @"Server=127.0.0.1;
                                          Port=5432;
                                          User Id=postgres;
                                          Password=01a039e6f2e530eb1a013720590055d6;
                                          database=gust;
                                          Enlist=true;
                                          MinPoolSize=0;Timeout=20;CommandTimeout=20;PersistSecurityInfo=true";

        public static BloggerContext CreateWithNpgsql()
        {        
            var options = new DbContextOptionsBuilder<BloggerContext>()
              .UseNpgsql(connectionString: pgsqlConnString)
              .Options;

            var ctx = new BloggerContext(options);

            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
            return ctx;
        }

        public static BloggerContext CreateWithInMemoryProvider(string dbname = "default")
        {
            var options = new DbContextOptionsBuilder<BloggerContext>()
           .UseInMemoryDatabase(databaseName: "inmemory_" + dbname)
           .Options;

            var ctx = new BloggerContext(options);

            return ctx;
        }

        /// <summary>
        /// The provider in this case is also "in memory" but it's a sqlite databse so we can
        /// use relational features.
        /// </summary>
        public static BloggerContext CreateWithSqliteProvider(string dbname = "default")
        {
            // In-memory database only exists while the connection is open
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<BloggerContext>()
                 .UseSqlite(connection)
                 .Options;

            var context = new BloggerContext(options);
            context.Database.EnsureCreated();

            return context;
        }

        public BloggerContext()
        { }

        public BloggerContext(DbContextOptions<BloggerContext> options)
        : base(options)
        { }

        public DbSet<Blog> Blogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>();
            modelBuilder.Entity<Blog>();
            modelBuilder.Entity<Post>();
            modelBuilder.Entity<PostVote>();
            modelBuilder.Entity<User>();

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(pgsqlConnString);
            }
        }
    }
}
