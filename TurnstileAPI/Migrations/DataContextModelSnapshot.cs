﻿// <auto-generated />
using System;
using InoosterTurnstileAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InoosterTurnstileAPI.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("InoosterTurnstileAPI.Models.GetEntryExit", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id"));

                    b.Property<string>("address")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("allowable")
                        .HasColumnType("integer");

                    b.Property<int>("cardid")
                        .HasColumnType("integer");

                    b.Property<DateTime>("date_time")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("department")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("tanimlama")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("users")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("workno")
                        .HasColumnType("integer");

                    b.HasKey("id");

                    b.ToTable("entryexits");
                });
#pragma warning restore 612, 618
        }
    }
}
