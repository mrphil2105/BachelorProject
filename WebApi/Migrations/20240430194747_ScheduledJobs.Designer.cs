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
    [Migration("20240430194747_ScheduledJobs")]
    partial class ScheduledJobs
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

                    b.Property<DateTimeOffset?>("EndDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Payload")
                        .HasColumnType("TEXT");

                    b.Property<string>("Result")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("ScheduleDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("StartDate")
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

                    b.Property<byte[]>("IdentityCommitment")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ReviewRandomness")
                        .IsRequired()
                        .HasColumnType("BLOB");

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

                    b.HasKey("Id");

                    b.ToTable("Submissions");
                });
#pragma warning restore 612, 618
        }
    }
}
