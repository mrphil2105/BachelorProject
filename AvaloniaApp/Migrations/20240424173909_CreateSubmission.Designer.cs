﻿// <auto-generated />
using System;
using Apachi.AvaloniaApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Apachi.AvaloniaApp.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240424173909_CreateSubmission")]
    partial class CreateSubmission
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("Apachi.AvaloniaApp.Data.Submission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("EncryptedSecrets")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("SecretsHmac")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("SubmissionCommitmentSignature")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Submissions");
                });

            modelBuilder.Entity("Apachi.AvaloniaApp.Data.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("AuthenticationHash")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("PasswordSalt")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<int>("Role")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Apachi.AvaloniaApp.Data.Submission", b =>
                {
                    b.HasOne("Apachi.AvaloniaApp.Data.User", "User")
                        .WithMany("Submissions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Apachi.AvaloniaApp.Data.User", b =>
                {
                    b.Navigation("Submissions");
                });
#pragma warning restore 612, 618
        }
    }
}
