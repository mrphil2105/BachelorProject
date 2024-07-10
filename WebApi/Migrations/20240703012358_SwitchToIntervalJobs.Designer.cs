﻿// <auto-generated />
using System;
using Apachi.WebApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Apachi.WebApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240703012358_SwitchToIntervalJobs")]
    partial class SwitchToIntervalJobs
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("Apachi.WebApi.Data.Job", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("CompletedDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Payload")
                        .HasColumnType("TEXT");

                    b.Property<string>("Result")
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Jobs");
                });

            modelBuilder.Entity("Apachi.WebApi.Data.JobSchedule", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan>("Interval")
                        .HasColumnType("TEXT");

                    b.Property<string>("JobType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("LastRun")
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("JobType")
                        .IsUnique();

                    b.ToTable("JobSchedules");
                });

            modelBuilder.Entity("Apachi.WebApi.Data.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ReviewerId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("SubmissionId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ReviewerId");

                    b.HasIndex("SubmissionId");

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("Apachi.WebApi.Data.Reviewer", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("EncryptedSharedKey")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ReviewerPublicKey")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.HasKey("Id");

                    b.ToTable("Reviewers");
                });

            modelBuilder.Entity("Apachi.WebApi.Data.Submission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("ClosedDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("IdentityCommitment")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("PaperSignature")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ReviewCommitment")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ReviewNonce")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ReviewRandomness")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("SubmissionCommitment")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("SubmissionPublicKey")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("SubmissionRandomness")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("SubmissionSignature")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("UpdatedDate")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Submissions");
                });

            modelBuilder.Entity("Apachi.WebApi.Data.Review", b =>
                {
                    b.HasOne("Apachi.WebApi.Data.Reviewer", "Reviewer")
                        .WithMany("Reviews")
                        .HasForeignKey("ReviewerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Apachi.WebApi.Data.Submission", "Submission")
                        .WithMany("Reviews")
                        .HasForeignKey("SubmissionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Reviewer");

                    b.Navigation("Submission");
                });

            modelBuilder.Entity("Apachi.WebApi.Data.Reviewer", b =>
                {
                    b.Navigation("Reviews");
                });

            modelBuilder.Entity("Apachi.WebApi.Data.Submission", b =>
                {
                    b.Navigation("Reviews");
                });
#pragma warning restore 612, 618
        }
    }
}
