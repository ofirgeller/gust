using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NodaTime;

namespace GustEfcConsumer.Model
{
    public class BloggerContextInMemory : DbContext
    {
        static readonly LoggerFactory MyLoggerFactory = new LoggerFactory(new[] { new ConsoleLoggerProvider((_, __) => true, true) });

        public static BloggerContextInMemory CreateWithInMemoryProvider(string dbname = "default")
        {
            var options = new DbContextOptionsBuilder<BloggerContextPg>()
           .UseInMemoryDatabase(databaseName: "inmemory_" + dbname)
           .Options;

            var ctx = new BloggerContextInMemory(options);

            return ctx;
        }

        BloggerContextInMemory(DbContextOptions options)
           : base(options)
        {
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostAudit> PostAudits { get; set; }
        public DbSet<PostVote> PostVotes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>();

            modelBuilder.Entity<PostAudit>();

            modelBuilder.Entity<User>();

            /// Only the npgsql provider knows how to map NodaTime entities, so when another provider is used
            /// we need extra mapping to map DateTime into Instant

            modelBuilder.Entity<Blog>(o =>
            {
                o.Property(e => e.CreatedAt)
                .HasConversion(i => i.ToDateTimeUtc(), i => Instant.FromDateTimeUtc(i));
            });

            modelBuilder.Entity<Post>(o =>
            {
                o.Property(e => e.CreatedAt)
                .HasConversion(i => i.ToDateTimeUtc(), i => Instant.FromDateTimeUtc(i));
            });

            modelBuilder.Entity<PostVote>(o =>
            {
                o.Property(e => e.CreatedAt)
                .HasConversion(i => i.ToDateTimeUtc(), i => Instant.FromDateTimeUtc(i));
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
            }
        }
    }

    public class BloggerContextSqlite : DbContext
    {
        static readonly LoggerFactory MyLoggerFactory = new LoggerFactory(new[] { new ConsoleLoggerProvider((_, __) => true, true) });

        /// <summary>
        /// The provider in this case is also "in memory" but it's a sqlite databse so we can
        /// use relational features.
        /// </summary>
        public static BloggerContextSqlite Create(string dbname = "default")
        {
            // In-memory database only exists while the connection is open
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<BloggerContextSqlite>()
                 .UseSqlite(connection)
                 .Options;

            var context = new BloggerContextSqlite(options);
            context.Database.EnsureCreated();

            return context;
        }

        public BloggerContextSqlite()
        { }

        public BloggerContextSqlite(DbContextOptions<BloggerContextSqlite> options)
        : base(options)
        {
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostAudit> PostAudits { get; set; }
        public DbSet<PostVote> PostVotes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>();

            modelBuilder.Entity<PostAudit>();

            modelBuilder.Entity<User>();

            modelBuilder.Entity<Blog>(o =>
            {
                o.Property(e => e.CreatedAt)
                .HasConversion(i => i.ToDateTimeUtc(), i => Instant.FromDateTimeUtc(i));
            });

            modelBuilder.Entity<Post>(o =>
            {
                o.Property(e => e.CreatedAt)
                .HasConversion(i => i.ToDateTimeUtc(), i => Instant.FromDateTimeUtc(i));
            });

            modelBuilder.Entity<PostVote>(o =>
            {
                o.Property(e => e.CreatedAt)
                .HasConversion(i => i.ToDateTimeUtc(), i => Instant.FromDateTimeUtc(i));
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {

            }
        }
    }

    public class BloggerContextPg : DbContext
    {
        static readonly LoggerFactory MyLoggerFactory = new LoggerFactory(new[] { new ConsoleLoggerProvider((_, __) => true, true) });

        static string pgsqlConnString = @"Server=127.0.0.1;
                                          Port=5432;
                                          User Id=postgres;
                                          Password=01a039e6f2e530eb1a013720590055d6;
                                          database=gust;
                                          Enlist=true;
                                          MinPoolSize=0;Timeout=20;CommandTimeout=20;PersistSecurityInfo=true";

        public static BloggerContextPg CreateWithNpgsql()
        {
            var options = new DbContextOptionsBuilder<BloggerContextPg>()
              .UseNpgsql(connectionString: pgsqlConnString, o => o.UseNodaTime())
              .Options;

            var ctx = new BloggerContextPg(options);

            return ctx;
        }

        public BloggerContextPg()
        { }

        public BloggerContextPg(DbContextOptions<BloggerContextPg> options)
        : base(options)
        {
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostAudit> PostAudits { get; set; }
        public DbSet<PostVote> PostVotes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>();
            modelBuilder.Entity<Blog>();
            modelBuilder.Entity<Post>();
            modelBuilder.Entity<PostAudit>();
            modelBuilder.Entity<PostVote>();
            modelBuilder.Entity<Comment>();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(pgsqlConnString, o => o.UseNodaTime());
            }
        }
    }
}
