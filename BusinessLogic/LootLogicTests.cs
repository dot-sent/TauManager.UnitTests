using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Moq;
using TauManager.BusinessLogic;
using Xunit;
using Xunit.Abstractions;

namespace TauManager.UnitTests.BusinessLogic
{
    public class LootLogicTests
    {
        private Mock<ICampaignLogic> _campaignLogicMock { get; set; }
        private ITestOutputHelper _output { get; set; }
        
        public LootLogicTests(ITestOutputHelper output)
        {
            _output = output;
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
                            AvailableToOtherSyndicates = true,
                        },
                        new Models.CampaignLoot
                        {
                            Id = 4,
                            CampaignId = 2,
                            ItemId = 3,
                            Status = Models.CampaignLoot.CampaignLootStatus.Undistributed,
                            AvailableToOtherSyndicates = true,
                        },
                        new Models.CampaignLoot
                        {
                            Id = 5,
                            CampaignId = 1,
                            ItemId = 3,
                            Status = Models.CampaignLoot.CampaignLootStatus.Other,
                        },
                        new Models.CampaignLoot
                        {
                            Id = 6,
                            CampaignId = 1,
                            ItemId = 3,
                            Status = Models.CampaignLoot.CampaignLootStatus.StaysWithSyndicate,
                            AvailableToOtherSyndicates = true,
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
                    context.Syndicate.Add(new Models.Syndicate
                    {
                        Id = 2,
                        Tag = "TTU",
                    });
                    context.Player.Add(new Models.Player{
                        Id = 4,
                        Name = "Player4",
                        SyndicateId = 2,
                    });
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
                Assert.Equal(4, result.AllCampaignLoot[1].Count());
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
        [InlineData("NonExistentPlayer", 5, null, "Manual drop", false)]
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

        [Theory]
        [InlineData("Correct", 2, Models.CampaignLoot.CampaignLootStatus.Other, true)]
        [InlineData("NonExistentCampaignLoot", 7, Models.CampaignLoot.CampaignLootStatus.Other, false)]
        public async void Test_SetLootStatusAsync_Theory(string caption, int id, Models.CampaignLoot.CampaignLootStatus status,
            bool expectedResult)
        {
            var options = _setupLootTestContext("Test_SetLootStatusAsync_Theory" + caption);
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = await lootLogic.SetLootStatusAsync(id, status);
                Assert.Equal(result, expectedResult);
                if (result)
                {
                    var campaignLoot = context.CampaignLoot.SingleOrDefault(cl => cl.Id == id);
                    Assert.Equal(status, campaignLoot.Status);
                }
            }
        }

        [Theory]
        [InlineData("CorrectNonZero", 2, 1, true, 1)]
        [InlineData("CorrectZero", 2, 0, true, null)]
        [InlineData("NonExistentCampaignLoot", 7, 1, false)]
        [InlineData("NonExistentPlayer", 1, 5, false)]
        public async void Test_SetLootHolderAsync_Theory(string caption, int lootId, int playerId,
            bool expectedResult, int? expectedHolderId = null)
        {
            var options = _setupLootTestContext("Test_SetLootHolderAsync_Theory" + caption);
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = await lootLogic.SetLootHolderAsync(lootId, playerId);
                Assert.Equal(expectedResult, result);
                if (result)
                {
                    var campaignLoot = context.CampaignLoot.SingleOrDefault(cl => cl.Id == lootId);
                    Assert.Equal(expectedHolderId, campaignLoot.HolderId);
                }
            }
        }

        [Theory]
        [InlineData("NonExistentCampaignLoot", 7, 1, 1, false)]
        [InlineData("NonExistentPlayer", 1, 5, 1, false)]
        [InlineData("WrongPlayerSyndicate", 1, 4, 1, false)]
        [InlineData("CorrectRequestExists", 2, 1, 1, true, 1, true)]
        [InlineData("CorrectRequestDoesNotExist", 2, 3, 1, true)]
        public void Test_CreateNewLootApplication_Theory(string caption, int lootId, int playerId, int? currentPlayerId,
            bool successExpected, int? expectedLootRequestId = 0, bool expectedExistingRequest = false)
        {
            var options = _setupLootTestContext("Test_CreateNewLootApplication_Theory" + caption);
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = lootLogic.CreateNewLootApplication(lootId, playerId, currentPlayerId);
                Assert.True(successExpected ? result != null : result == null);
                if (successExpected) {
                    Assert.Equal(expectedLootRequestId, result.Request.Id);
                    Assert.Equal(lootId, result.Loot.Id);
                    Assert.Equal(expectedExistingRequest, result.RequestExists);
                }
            }
        }

        [Theory]
        [InlineData("CorrectCreate", 1, 3, null, 1, false, false, true)]
        [InlineData("NonExistentCampaignLoot", 7, 3, null, 1, false, false, false)]
        [InlineData("NonExistentPlayer", 1, 5, null, 1, false, false, false)]
        [InlineData("WrongPlayerSyndicate", 1, 4, null, 1, false, false, false)]
        [InlineData("CorrectDelete", 2, 1, null, 1, false, true, true)]
        [InlineData("DeleteNonExistentRequest", 1, 1, null, 1, false, true, false)]
        [InlineData("CorrectUpdateAddSpecialRequest", 2, 1, "Test Special Request", 1, true, false, true)]
        [InlineData("CorrectUpdateRemoveSpecialRequest", 2, 2, null, 2, false, false, true)]
        public async void Test_ApplyForLoot_Theory(string caption, int lootId, int playerId, string comments, int? currentPlayerId, bool specialOffer, bool deleteRequest,
            bool expectedResult)
        {
            var options = _setupLootTestContext("Test_ApplyForLoot_Theory" + caption);
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = await lootLogic.ApplyForLoot(lootId, playerId, comments, currentPlayerId, specialOffer, deleteRequest);
                Assert.Equal(expectedResult, result);
                if (expectedResult)
                {
                    var lootRequest = context.LootRequest.SingleOrDefault(lr => lr.LootId == lootId && lr.RequestedForId == playerId);
                    if (deleteRequest)
                    {
                        Assert.Null(lootRequest);
                    } else {
                        Assert.NotNull(lootRequest);
                        Assert.Equal(comments, lootRequest.SpecialOfferDescription);
                        Assert.Equal(specialOffer ? Models.LootRequest.LootRequestStatus.SpecialOffer
                            : Models.LootRequest.LootRequestStatus.Interested, lootRequest.Status);
                    }
                }
            }
        }

        [Theory]
        [InlineData("NonExistentCampaignLoot",               1, 1, 7,  0,  0, null,                   false, false)]
        [InlineData("NonExistentPlayer",                     5, 1, 1,  0,  0, null,                   false, false)]
        [InlineData("DeleteNonExistentRequest",              1, 1, 1, -1,  0, null,                   false, false)]
        [InlineData("CreateCorrectRequestCreateOnly",        3, 3, 1,  0, -1, null,                   false, true )]
        [InlineData("CreateCorrectRequestAndAwardDontDrop",  3, 3, 1,  1,  3, null,                   false, true )]
        [InlineData("CreateCorrectRequestAndAwardDrop",      3, 3, 1,  1,  3, null,                    true, true )]
        [InlineData("DeleteCorrectRequestDontDrop",          1, 1, 2, -1, -1, null,                   false, true )]
        [InlineData("UpdateCorrectRequestDontAwardDontDrop", 1, 1, 2,  2, -1, "Test special request", false, true )]
        [InlineData("UpdateCorrectRequestAwardDontDrop",     1, 1, 2,  1,  2, null,                   false, true )]
        public async void Test_SetLootRequestStatus_Theory(string caption, int playerId, int currentPlayerId, int campaignLootId,
            int status, int lootStatus, string comments, bool dropRequestorDown,
            bool expectedResult)
        {
            var options = _setupLootTestContext("Test_SetLootRequestStatus_Theory" + caption);
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = await lootLogic.SetLootRequestStatus(playerId, currentPlayerId, campaignLootId, status, lootStatus, comments, dropRequestorDown);
                Assert.Equal(expectedResult, result);
                if (status > -1 && expectedResult)
                {
                    var testRequest = context.LootRequest.SingleOrDefault(lr => lr.RequestedForId == playerId && lr.LootId == campaignLootId);
                    Assert.NotNull(testRequest);
                    Assert.Equal((Models.LootRequest.LootRequestStatus)status, testRequest.Status);
                    Assert.Equal(comments, testRequest.SpecialOfferDescription);
                    var testLoot = context.CampaignLoot.SingleOrDefault(cl => cl.Id == campaignLootId);
                    Assert.NotNull(testLoot);
                    if (lootStatus > -1)
                    {
                        Assert.Equal((Models.CampaignLoot.CampaignLootStatus)lootStatus, testLoot.Status);
                        Assert.Equal(playerId, testLoot.HolderId);
                    }
                    if (dropRequestorDown)
                    {
                        Assert.Equal(4, context.PlayerListPositionHistory.Count());
                        var lastHistoryEntry = context.PlayerListPositionHistory.LastOrDefault();
                        Assert.NotNull(lastHistoryEntry);
                        Assert.Equal("Drop associated with loot request", lastHistoryEntry.Comment);
                        Assert.Equal(playerId, lastHistoryEntry.PlayerId);
                        Assert.NotNull(lastHistoryEntry.LootRequest);
                        Assert.Equal(campaignLootId, lastHistoryEntry.LootRequest.LootId);
                        Assert.True(lastHistoryEntry.CreatedAt > DateTime.Now.AddMinutes(-1) && lastHistoryEntry.CreatedAt < DateTime.Now.AddMinutes(1));
                    }
                }
            }
        }

        [Theory]
        [InlineData("GetCorrectOverviewSameSyndicate", new int[]{0,1,2,3,4}, 1, 6, 0)]
        [InlineData("GetCorrectOverviewOtherSyndicate", new int[]{0,1,2,3,4}, 2, 0, 3)]
        [InlineData("GetCorrectOverviewNullDisplay", null, 1, 6, 0)]
        public void Test_GetOverview_Theory(string caption, int[] display, int syndicateId, 
            int expectedLootCount, int expectedOSLootCount)
        {
            var options = _setupLootTestContext("Test_GetOverview_Theory" + caption);
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = lootLogic.GetOverview(display, 0, 0, syndicateId);
                Assert.NotNull(result);
                Assert.Equal(expectedLootCount, result.AllLoot.Count());
                Assert.Equal(expectedOSLootCount, result.OtherSyndicatesLoot.Count());
                Assert.Equal(5, result.LootStatuses.Keys.Count);
                if (display == null)
                {
                    Assert.Equal(new int[]{0,1,2,3,4}, result.Display);
                } else {
                    Assert.Equal(display, result.Display);
                }
            }
        }

        [Theory]
        [InlineData("GetCorrectInfo", 2, 2)]
        public void Test_GetLootRequestsInfoTheory(string caption, int campaignLootId,
            int? expectedLootRequestCount)
        {
            var options = _setupLootTestContext("Test_GetLootRequestsInfoTheory" + caption);
            using (var context = new TauDbContext(options))
            {
                var lootLogic = new LootLogic(context, _campaignLogicMock.Object);
                var result = lootLogic.GetLootRequestsInfo(campaignLootId);
                Assert.Equal(expectedLootRequestCount, result == null || result.Requests == null ? null : (int?)result.Requests.Count());
            }
        }
    }
}