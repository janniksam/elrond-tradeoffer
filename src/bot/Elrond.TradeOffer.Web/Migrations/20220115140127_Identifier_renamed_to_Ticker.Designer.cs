﻿// <auto-generated />
using System;
using Elrond.TradeOffer.Web.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Elrond.TradeOffer.Web.Migrations
{
    [DbContext(typeof(ElrondTradeOfferDbContext))]
    [Migration("20220115140127_Identifier_renamed_to_Ticker")]
    partial class Identifier_renamed_to_Ticker
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Elrond.TradeOffer.Web.Database.DbBid", b =>
                {
                    b.Property<Guid>("OfferId")
                        .HasColumnType("char(36)");

                    b.Property<long>("CreatorUserId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("CreatorChatId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.Property<string>("TokenAmount")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("TokenName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong>("TokenNonce")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("TokenPrecision")
                        .HasColumnType("int");

                    b.Property<string>("TokenTicker")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("OfferId", "CreatorUserId");

                    b.HasIndex("CreatorUserId");

                    b.ToTable("Bids");
                });

            modelBuilder.Entity("Elrond.TradeOffer.Web.Database.DbFeatureState", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255)");

                    b.Property<long?>("ChangedById")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<DateTime>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("ChangedById");

                    b.ToTable("FeatureStates");
                });

            modelBuilder.Entity("Elrond.TradeOffer.Web.Database.DbOffer", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("CreatorChatId")
                        .HasColumnType("bigint");

                    b.Property<long>("CreatorUserId")
                        .HasColumnType("bigint");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Network")
                        .HasColumnType("int");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<string>("TokenAmount")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("TokenName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong>("TokenNonce")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("TokenPrecision")
                        .HasColumnType("int");

                    b.Property<string>("TokenTicker")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("CreatorUserId");

                    b.ToTable("Offers");
                });

            modelBuilder.Entity("Elrond.TradeOffer.Web.Database.DbUser", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<string>("Address")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Network")
                        .HasColumnType("int");

                    b.Property<DateTime>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.Property<DateTime>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Elrond.TradeOffer.Web.Database.DbBid", b =>
                {
                    b.HasOne("Elrond.TradeOffer.Web.Database.DbUser", "CreatorUser")
                        .WithMany("Bids")
                        .HasForeignKey("CreatorUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Elrond.TradeOffer.Web.Database.DbOffer", "Offer")
                        .WithMany("Bids")
                        .HasForeignKey("OfferId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CreatorUser");

                    b.Navigation("Offer");
                });

            modelBuilder.Entity("Elrond.TradeOffer.Web.Database.DbFeatureState", b =>
                {
                    b.HasOne("Elrond.TradeOffer.Web.Database.DbUser", "ChangedBy")
                        .WithMany()
                        .HasForeignKey("ChangedById");

                    b.Navigation("ChangedBy");
                });

            modelBuilder.Entity("Elrond.TradeOffer.Web.Database.DbOffer", b =>
                {
                    b.HasOne("Elrond.TradeOffer.Web.Database.DbUser", "CreatorUser")
                        .WithMany("Offers")
                        .HasForeignKey("CreatorUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CreatorUser");
                });

            modelBuilder.Entity("Elrond.TradeOffer.Web.Database.DbOffer", b =>
                {
                    b.Navigation("Bids");
                });

            modelBuilder.Entity("Elrond.TradeOffer.Web.Database.DbUser", b =>
                {
                    b.Navigation("Bids");

                    b.Navigation("Offers");
                });
#pragma warning restore 612, 618
        }
    }
}
