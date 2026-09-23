namespace Adr.Semantics.Data
{
    using Microsoft.EntityFrameworkCore;

    internal sealed class GlossaryDbContext : DbContext
    {
        public GlossaryDbContext(DbContextOptions<GlossaryDbContext> options)
            : base(options) { }

        public DbSet<GlossaryEntity> Glossaries => Set<GlossaryEntity>();

        public DbSet<GlossaryReleaseEntity> GlossaryReleases => Set<GlossaryReleaseEntity>();

        public DbSet<GlossaryTermEntity> GlossaryTerms => Set<GlossaryTermEntity>();

        public DbSet<GlossaryTermRevisionEntity> GlossaryTermRevisions =>
            Set<GlossaryTermRevisionEntity>();

        public DbSet<GlossaryReleaseTermEntity> GlossaryReleaseTerms =>
            Set<GlossaryReleaseTermEntity>();

        public DbSet<GlossaryImportEntity> GlossaryImports => Set<GlossaryImportEntity>();

        public DbSet<GlossaryTermSubmissionEntity> GlossaryTermSubmissions =>
            Set<GlossaryTermSubmissionEntity>();

        public DbSet<GlossaryDraftEntity> GlossaryDrafts => Set<GlossaryDraftEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GlossaryEntity>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Id).HasMaxLength(200);
                entity.Property(item => item.Name).HasMaxLength(500);
                entity
                    .HasOne(item => item.CurrentRelease)
                    .WithMany()
                    .HasForeignKey(item => item.CurrentReleaseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<GlossaryReleaseEntity>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Version).HasMaxLength(50);
                entity.Property(item => item.SourceAsset).HasMaxLength(500);
                entity.Property(item => item.SourceHash).HasMaxLength(64);
                entity.HasIndex(item => new { item.GlossaryId, item.Version }).IsUnique();
                entity.HasIndex(item => new { item.GlossaryId, item.Sequence }).IsUnique();
                entity
                    .HasOne(item => item.Glossary)
                    .WithMany(item => item.Releases)
                    .HasForeignKey(item => item.GlossaryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GlossaryTermEntity>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.GlossaryId).HasMaxLength(200);
                entity.Property(item => item.Name).HasMaxLength(500);
                entity.HasIndex(item => new { item.GlossaryId, item.Name }).IsUnique();
            });

            modelBuilder.Entity<GlossaryTermRevisionEntity>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Name).HasMaxLength(500);
                entity.Property(item => item.Term).HasMaxLength(500);
                entity.Property(item => item.SchemaType).HasMaxLength(50);
                entity.Property(item => item.ContentHash).HasMaxLength(64);
                entity.HasIndex(item => item.StaticId);
                entity.HasIndex(item => new { item.TermId, item.Revision }).IsUnique();
                entity
                    .HasOne(item => item.TermIdentity)
                    .WithMany(item => item.Revisions)
                    .HasForeignKey(item => item.TermId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GlossaryReleaseTermEntity>(entity =>
            {
                entity.HasKey(item => new { item.ReleaseId, item.TermRevisionId });
                entity
                    .HasOne(item => item.Release)
                    .WithMany(item => item.Terms)
                    .HasForeignKey(item => item.ReleaseId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity
                    .HasOne(item => item.TermRevision)
                    .WithMany(item => item.Releases)
                    .HasForeignKey(item => item.TermRevisionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<GlossaryImportEntity>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.SourceAsset).HasMaxLength(500);
                entity.Property(item => item.SourceHash).HasMaxLength(64);
                entity.Property(item => item.Status).HasMaxLength(50);
                entity
                    .HasIndex(item => new
                    {
                        item.GlossaryId,
                        item.SourceAsset,
                        item.SourceHash,
                    })
                    .IsUnique();
            });

            modelBuilder.Entity<GlossaryTermSubmissionEntity>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.SourceType).HasMaxLength(50);
                entity.Property(item => item.SourceReference).HasMaxLength(500);
                entity.Property(item => item.Operation).HasMaxLength(20);
                entity.Property(item => item.Name).HasMaxLength(500);
                entity.Property(item => item.SubmittedStaticId).HasMaxLength(100);
                entity.HasIndex(item => new { item.GlossaryId, item.Name });
                entity
                    .HasOne(item => item.Import)
                    .WithMany(item => item.TermSubmissions)
                    .HasForeignKey(item => item.ImportId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity
                    .HasOne(item => item.Draft)
                    .WithMany(item => item.TermSubmissions)
                    .HasForeignKey(item => item.DraftId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity
                    .HasOne(item => item.TermRevision)
                    .WithMany()
                    .HasForeignKey(item => item.TermRevisionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<GlossaryDraftEntity>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.GlossaryId).HasMaxLength(200);
                entity.Property(item => item.Status).HasMaxLength(30);
                entity
                    .HasOne(item => item.BaseRelease)
                    .WithMany()
                    .HasForeignKey(item => item.BaseReleaseId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity
                    .HasOne(item => item.PublishedRelease)
                    .WithMany()
                    .HasForeignKey(item => item.PublishedReleaseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
