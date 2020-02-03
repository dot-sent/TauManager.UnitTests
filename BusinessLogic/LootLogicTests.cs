using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Moq;
using TauManager.BusinessLogic;
using Xunit;

namespace TauManager.UnitTests.BusinessLogic
{
    public class LootLogicTests
    {
        private Mock<ICampaignLogic> _campaignLogicMock { get; set; }
        
        public LootLogicTests()
        {
            _campaignLogicMock = new Mock<ICampaignLogic>();
            _campaignLogicMock.Setup(cl => cl.GetCampaignAttendance(null, 1)).Returns(
                new ViewModels.AttendanceViewModel
                {
                    TotalAttendance = new Dictionary<int, int>(),
                    Last10T5HardAttendance = new Dictionary<int, int>(),
                }
            );
        }

        private DbContextOptions<TauDbContext> _setupLootTestContext([CallerMemberName]string databaseName = "", bool empty = false)
        {
            var options = new DbContextOptionsBuilder<TauDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;
            using (var context = new TauDbContext(options))
            {
                if (!empty)
                {
                    context.Syndicate.Add(new Models.Syndicate{
                        Id = 1,
                        Tag = "TAU"
                    });
                    context.Player.Add(new Models.Player{
                        Id = 1,
                        Name = "Leader",
                        Active = true,
                        SyndicateId = 1,
                        Level = 5.2M,
                    });
                    context.Player.Add(new Models.Player{
                        Id = 2,
                        Name = "Player1",
                        Active = true,
                        SyndicateId = 1,
                        Level = 24,
                    });
                    context.Player.Add(new Models.Player{
                        Id = 3,
                        Name = "Player2",
                        Active = true,
                        SyndicateId = 1,
                        Level = 25,
                    });
                    context.Campaign.Add(new Models.Campaign{
                        Id = 1,
                        Station = "Yards of Gadani",
                        Name = "Campaign #1",
                        Comments = "Test comments",
                        Difficulty = Models.Campaign.CampaignDifficulty.Normal,
                        Tiers = 31,
                        SyndicateId = 1,
                        Status = Models.Campaign.CampaignStatus.Completed,
                        UTCDateTime = DateTime.Parse("2019-12-31 00:00:00"),
                        ManagerId = 1,
                    });
                    context.Campaign.Add(new Models.Campaign{
                        Id = 2,
                        Station = "Yards of Gadani",
                        Name = "Campaign #2",
                        Comments = "Test comments",
                        Difficulty = Models.Campaign.CampaignDifficulty.Hard,
                        Tiers = 31,
                        SyndicateId = 1,
                        Status = Models.Campaign.CampaignStatus.Completed,
                        UTCDateTime = DateTime.Parse("2020-01-02 00:00:00"),
                        ManagerId = 1,
                    });
                    context.CampaignAttendance.AddRange(
                        new Models.CampaignAttendance{
                            PlayerId = 1,
                            CampaignId = 1,
                        },
                        new Models.CampaignAttendance{
                            PlayerId = 1,
                            CampaignId = 2,
                        },
                        new Models.CampaignAttendance{
                            PlayerId = 2,
                            CampaignId = 1,
                        },
                        new Models.CampaignAttendance{
                            PlayerId = 3,
                            CampaignId = 2,
                        }
                    );
                    context.Item.AddRange(
                        new Models.Item
                        {
                            Id = 1,
                            Name = "Test1",
                            Tier = 1,
                            Type = Models.Item.ItemType.Weapon,
                            Rarity = Models.Item.ItemRarity.Epic,
                        },
                        new Models.Item
                        {
                            Id = 2,
                            Name = "Test2",
                            Tier = 2,
                            Type = Models.Item.ItemType.Weapon,
                            Rarity = Models.Item.ItemRarity.Epic,
                        },
                        new Models.Item
                        {
                            Id = 3,
                            Name = "Test3",
                            Tier = 2,
                            Type = Models.Item.ItemType.Armor,
                            Rarity = Models.Item.ItemRarity.Epic,
                        }
                    );
                    context.CampaignLoot.AddRange(
                        new Models.CampaignLoot
                        {
                            Id = 1,
                            CampaignId = 1,
                            ItemId = 2,
                            Status = Models.CampaignLoot.CampaignLootStatus.Undistributed,
                        },
                        new Models.CampaignLoot
                        {
                            Id = 2,
                            CampaignId = 1,
                            ItemId = 1,
                            Status = Models.CampaignLoot.CampaignLootStatus.Undistributed,
                        },
                        new Models.CampaignLoot
                        {
                            Id = 3,
                            CampaignId = 2,
                            ItemId = 1,
                            Status = Models.CampaignLoot.CampaignLootStatus.Undistributed,
                        },
                        new Models.CampaignLoot
                        {
                            Id = 4,
                            CampaignId = 2,
                            ItemId = 3,
                            Status = Models.CampaignLoot.CampaignLootStatus.Undistributed,
                        }
                    );
                    context.LootRequest.AddRange(
                        new Models.LootRequest
                        {
                            Id = 1,
                            LootId = 2,
                            RequestedById = 1,
                            RequestedForId = 1,
                            Status = Models.LootRequest.LootRequestStatus.Interested,
                        },
                        new Models.LootRequest
                        {
                            Id = 2,
                            LootId = 2,
                            RequestedById = 2,
                            RequestedForId = 2,
                            Status = Models.LootRequest.LootRequestStatus.SpecialOffer,
                            SpecialOfferDescription = "Special offer description stub",
                        }
                    );
                    context.PlayerListPositionHistory.AddRange(
                        new Models.PlayerListPositionHistory
                        {
                            PlayerId = 1,
                            LootRequestId = null,
                            Comment = "Initial seed",
                            Id = 1,
                        },
                        new Models.PlayerListPositionHistory
                        {
                            PlayerId = 2,
                            LootRequestId = null,
                            Comment = "Initial seed",
                            Id = 2,
                        },
                        new Models.PlayerListPositionHistory
                        {
                            PlayerId = 3,
                            LootRequestId = null,
                            Comment = "Initial seed",
                            Id = 3,
                        }
                    );
                    context.SaveChanges();
                }
            }
            return options;
        }

        [Fact]
        public void Test_GetCurrentDistributionOrder_CorrectEmpty()
        {
            var options = _setupLootTestContext(empty: true);
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = lootLogic.GetCurrentDistributionOrder(null, false, false, 1, null);
                Assert.Empty(result.CurrentOrder);
                Assert.Null(result.CurrentPlayer);
                Assert.Empty(result.AllPlayers);
                Assert.Empty(result.AllCampaignLoot);
                Assert.Empty(result.AllLootRequests);
                Assert.Empty(result.AllCampaigns);
                Assert.Equal(5, result.LootStatuses.Keys.Count);
                Assert.Equal("OnLoan", result.LootStatuses[3]);
                Assert.Null(result.CampaignId);
                Assert.False(result.UndistributedLootOnly);
                Assert.False(result.IncludeInactive);
                Assert.Empty(result.TotalAttendanceRate);
                Assert.Empty(result.HardT5AttendanceRate);
            }
        }

        [Fact]
        public void Test_GetCurrentDistributionOrder_CorrectFull()
        {
            var options = _setupLootTestContext();
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = lootLogic.GetCurrentDistributionOrder(null, false, false, 1, null);
                Assert.Equal(3, result.CurrentOrder.Count());
                Assert.Null(result.CurrentPlayer);
                Assert.Equal(3, result.AllPlayers.Count());
                Assert.Equal(2, result.AllCampaignLoot.Keys.Count);
                Assert.Equal(2, result.AllCampaignLoot[1].Count());
                Assert.True(result.AllCampaignLoot[1].First().Item.Tier < result.AllCampaignLoot[1].Last().Item.Tier);
                Assert.Equal(2, result.AllLootRequests.Keys.Count);
                Assert.Equal(2, result.AllCampaigns.Count);
                Assert.Equal(5, result.LootStatuses.Keys.Count);
                Assert.Equal("OnLoan", result.LootStatuses[3]);
                Assert.Null(result.CampaignId);
                Assert.False(result.UndistributedLootOnly);
                Assert.False(result.IncludeInactive);
                Assert.Empty(result.TotalAttendanceRate);
                Assert.Empty(result.HardT5AttendanceRate);
            }
        }

        [Theory]
        [InlineData("CorrectWithLootRequest", 1, 1, null, true, 4, 1, null, 1)]
        [InlineData("CorrectWithComment", 1, null, "Manual drop", true, 4, 1, "Manual drop", null)]
        [InlineData("NoCommentNoLootRequestId", 1, null, null, false)]
        [InlineData("NonExistentPlayer", 4, null, "Manual drop", false)]
        [InlineData("NonExistentLootRequest", 1, 4, null, false)]
        [InlineData("LootRequestPlayerMismatch", 1, 2, null, false)]
        public async void Test_AppendPlayerToBottomAsync_Theory(string caption, int playerId, int? lootRequestId, string comment,
            bool expectedResult, int expectedHistoryCount = 3, int expectedLastPlayerId = 3, string expectedLastComment = "Initial seed", int? expectedLastLootRequestId = null)
        {
            var options = _setupLootTestContext("Test_AppendPlayerToBottomAsync_Theory" + caption);
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = await lootLogic.AppendPlayerToBottomAsync(playerId, lootRequestId, comment);
                Assert.Equal(expectedResult, result);
                Assert.Equal(expectedHistoryCount, context.PlayerListPositionHistory.Count());
                var lastHistory = context.PlayerListPositionHistory.LastOrDefault();
                Assert.Equal(expectedLastPlayerId, lastHistory.PlayerId);
                Assert.Equal(expectedLastComment, lastHistory.Comment);
                Assert.Equal(expectedLastLootRequestId, lastHistory.LootRequestId);
            }
        }
    }
}