using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GenTreesCore.Entities
{
    public class ApplicationContext : DbContext
    {
        public DbSet<GenTree> GenTrees { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<GenTreeDateTimeSetting> GenTreeDateTimeSettings { get; set; }
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            /*
            if (!Database.CanConnect())
                throw new System.Exception("No connection to server");
            */
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnUserCreating(modelBuilder);
            OnGenTreeCreating(modelBuilder);
            OnDateTimeSettingCreating(modelBuilder);
            OnGenTreeDateTimeCreating(modelBuilder);
            OnEraCreating(modelBuilder);
            OnPersonCreating(modelBuilder);
            OnRelationCreating(modelBuilder);
            OnDescriptionTemplateCreating(modelBuilder);
            OnCustomDescriptionCreating(modelBuilder);       

            base.OnModelCreating(modelBuilder);
        }

        private void OnUserCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .ToTable("Users");

            modelBuilder.Entity<User>()
                .Property(e => e.Role)
                .HasConversion(v => v.ToString(), v => (Role)Enum.Parse(typeof(Role), v))
                .HasColumnName("Role");
        }

        private void OnGenTreeCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GenTree>()
                .ToTable("GenTrees");
        }

        private void OnDateTimeSettingCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GenTreeDateTimeSetting>()
                .ToTable("GenTreeDateTimeSettings");
        }

        private void OnEraCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GenTreeEra>()
                .ToTable("GenTreeEras");
        }

        private void OnGenTreeDateTimeCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GenTreeDateTime>()
                .ToTable("GenTreeDates");
        }

        private void OnPersonCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>()
                .ToTable("Persons");

            modelBuilder.Entity<Person>()
                .HasMany(e => e.Relations)
                .WithOne()
                .HasForeignKey("SourcePersonId");
        }

        private void OnRelationCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Relation>()
                .ToTable("Relations");

            modelBuilder.Entity<Relation>()
                .HasDiscriminator<string>("RelationType")
                .HasValue<ChildRelation>("ChildRelation")
                .HasValue<SpouseRelation>("SpouseRelation");

            modelBuilder.Entity<ChildRelation>()
                .Property(e => e.RelationRate)
                .HasConversion(v => v.ToString(), v => (RelationRate)Enum.Parse(typeof(RelationRate), v))
                .HasColumnName("RelationRate");
        }

        private void OnDescriptionTemplateCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomPersonDescriptionTemplate>()
                .ToTable("CustomPersonDescriptionTemplates");

            modelBuilder.Entity<CustomPersonDescriptionTemplate>()
                .Property(e => e.Type)
                .HasConversion(v => v.ToString(), v => (TemplateType)Enum.Parse(typeof(TemplateType), v))
                .HasColumnName("Type");
        }

        private void OnCustomDescriptionCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomPersonDescription>()
                .ToTable("CustomPersonDescriptions");

            modelBuilder.Entity<CustomPersonDescription>()
                .Property(e => e.Value)
                .HasConversion(v => JsonConvert.SerializeObject(v), v => JsonConvert.DeserializeObject(v));

            modelBuilder.Entity<CustomPersonDescription>()
                .HasOne(e => e.Template)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
