﻿// <auto-generated />
using System;
using Apachi.UserApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Apachi.UserApp.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240809223448_RenameToUserId")]
    partial class RenameToUserId
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.7");

            modelBuilder.Entity("Apachi.UserApp.Data.LogEvent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Identifier")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<int>("Step")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("LogEvents");
                });

            modelBuilder.Entity("Apachi.UserApp.Data.Reviewer", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("AuthenticationHash")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("EncryptedPrivateKey")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("EncryptedSharedKey")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("PasswordSalt")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Reviewers");
                });

            modelBuilder.Entity("Apachi.UserApp.Data.Submission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("EncryptedIdentityRandomness")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("EncryptedPrivateKey")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("EncryptedSubmissionKey")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<Guid>("SubmitterId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SubmitterId");

                    b.ToTable("Submissions");
                });

            modelBuilder.Entity("Apachi.UserApp.Data.Submitter", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("AuthenticationHash")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("PasswordSalt")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Submitters");
                });

            modelBuilder.Entity("Apachi.UserApp.Data.Submission", b =>
                {
                    b.HasOne("Apachi.UserApp.Data.Submitter", "Submitter")
                        .WithMany("Submissions")
                        .HasForeignKey("SubmitterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Submitter");
                });

            modelBuilder.Entity("Apachi.UserApp.Data.Submitter", b =>
                {
                    b.Navigation("Submissions");
                });
#pragma warning restore 612, 618
        }
    }
}
