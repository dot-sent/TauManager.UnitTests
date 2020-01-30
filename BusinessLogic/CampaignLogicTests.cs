using Xunit;
using TauManager.BusinessLogic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Moq;
using TauManager.Utils;
using System.Threading.Tasks;

namespace TauManager.UnitTests.BusinessLogic
{
    public class CampaignLogicTests
    {
        private Mock<ITauHeadClient> _tauHeadClientMock { get; set; }
        public CampaignLogicTests()
        {
            _tauHeadClientMock = new Mock<ITauHeadClient>();
            _tauHeadClientMock.Setup(th => th.GetItemData("Test1")).ReturnsAsync(new Models.Item{
                Name = "Test item 1",
                Slug = "test-item-1",
                Type = Models.Item.ItemType.Weapon,
                WeaponType = Models.Item.ItemWeaponType.Blade,
                WeaponRange = Models.Item.ItemWeaponRange.Short,
            });
        }

        private DbContextOptions<TauDbContext> _setupCampaignTestContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<TauDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            using (var context = new TauDbContext(options))
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
                });
                context.Player.Add(new Models.Player{
                    Id = 2,
                    Name = "Player1",
                    Active = true,
                    SyndicateId = 1,
                });
                context.Player.Add(new Models.Player{
                    Id = 3,
                    Name = "Player2",
                    Active = true,
                    SyndicateId = 1,
                });
                context.Campaign.Add(new Models.Campaign{
                    Id = 1,
                    Station = "Yards of Gadani",
                    Name = "Campaign #1",
                    Comments = "Test comments",
                    Difficulty = Models.Campaign.CampaignDifficulty.Normal,
                    Tiers = 31,
                    SyndicateId = 1,
                    Status = Models.Campaign.CampaignStatus.Planned,
                    UTCDateTime = DateTime.Parse("2020-12-31 00:00:00"),
                    ManagerId = 1,
                });
                context.SaveChanges();
            }

            return options;
        }

        [Fact]
        public void Test_GetCampaignOverview()
        {
            var options = _setupCampaignTestContext("Test_GetCampaignOverview");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = campaignLogic.GetCampaignOverview(1, false, false, false, 1);
                Assert.Empty(result.CurrentCampaigns);
                Assert.Single(result.FutureCampaigns);
                Assert.Empty(result.PastCampaigns);
                Assert.Equal(5, result.LootStatuses.Keys.Count);
                Assert.Empty(result.LootToDistribute);
            }
        }

        [Fact]
        public void Test_GetCampaignById()
        {
            var options = _setupCampaignTestContext("Test_GetCampaignById");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = campaignLogic.GetCampaignById(1, false, false, 1);
                Assert.Equal(3, result.Players.Count());
                Assert.NotNull(result.Campaign);
                Assert.Null(result.Loot);
                Assert.Equal(4, result.DifficultyLevels.Keys.Count);
                Assert.Equal(8, result.Statuses.Keys.Count);
                Assert.Empty(result.KnownEpics);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task Test_CreateOrEditCampaign_CreateCorrectCampaignAsync()
        {
            var options = _setupCampaignTestContext("Test_CreateOrEditCampaign_CreateCorrectCampaign");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                await campaignLogic.CreateOrEditCampaign(new Models.Campaign{
                    Station = "YoG",
                    Name = "Campaign #2",
                    Comments = "Test comment",
                    Difficulty = Models.Campaign.CampaignDifficulty.Extreme,
                    Tiers = 31,
                    SyndicateId = 1,
                    Status = Models.Campaign.CampaignStatus.Planned,
                    UTCDateTime = DateTime.Parse("2020-12-29 00:00:00"),
                    ManagerId = 2,
                }, 1);
                Assert.Equal(2, context.Campaign.Count());
                Assert.NotNull(context.Campaign.SingleOrDefault(c => c.Id == 2));
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task Test_CreateOrEditCampaign_EditCorrectCampaignAsync()
        {
            var options = _setupCampaignTestContext("Test_CreateOrEditCampaign_EditCorrectCampaignAsync");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                await campaignLogic.CreateOrEditCampaign(new Models.Campaign{
                    Id = 1,
                    Station = "YoG",
                    Name = "Campaign #1",
                    Comments = "Test comment updated",
                    Difficulty = Models.Campaign.CampaignDifficulty.Extreme,
                    Tiers = 31,
                    SyndicateId = 1,
                    Status = Models.Campaign.CampaignStatus.Planned,
                    UTCDateTime = DateTime.Parse("2020-12-29 00:00:00"),
                    ManagerId = 2,
                }, 1);
                Assert.Equal(1, context.Campaign.Count());
                var campaign = context.Campaign.SingleOrDefault(c => c.Id == 1);
                Assert.NotNull(campaign);
                Assert.Equal("Test comment updated", campaign.Comments);
                Assert.Equal(2, campaign.ManagerId);
                Assert.Equal("YoG", campaign.Station);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task Test_CreateOrEditCampaign_CreateCampaignSyndicateMismatchAsync()
        {
            var options = _setupCampaignTestContext("Test_CreateOrEditCampaign_CreateCampaignSyndicateMismatchAsync");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                await campaignLogic.CreateOrEditCampaign(new Models.Campaign{
                    Station = "YoG",
                    Name = "Campaign #2",
                    Comments = "Test comment",
                    Difficulty = Models.Campaign.CampaignDifficulty.Extreme,
                    Tiers = 31,
                    SyndicateId = 1,
                    Status = Models.Campaign.CampaignStatus.Planned,
                    UTCDateTime = DateTime.Parse("2020-12-29 00:00:00"),
                    ManagerId = 2,
                }, 2);
                Assert.Equal(1, context.Campaign.Count());
                Assert.Null(context.Campaign.SingleOrDefault(c => c.Id == 2));
            }
        }

        [Fact]
        public async void Test_CreateOrEditCampaign_EditNonExistentCampaignAsync()
        {
            var options = _setupCampaignTestContext("Test_CreateOrEditCampaign_EditNonExistentCampaignAsync");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                await campaignLogic.CreateOrEditCampaign(new Models.Campaign{
                    Id = 3,
                    Station = "YoG",
                    Name = "Campaign #2",
                    Comments = "Test comment",
                    Difficulty = Models.Campaign.CampaignDifficulty.Extreme,
                    Tiers = 31,
                    SyndicateId = 1,
                    Status = Models.Campaign.CampaignStatus.Planned,
                    UTCDateTime = DateTime.Parse("2020-12-29 00:00:00"),
                    ManagerId = 2,
                }, 1);
                Assert.Equal(1, context.Campaign.Count());
                Assert.Null(context.Campaign.SingleOrDefault(c => c.Id == 3));
            }
        }

        [Fact]
        public async void Test_AddLootByTauheadURL_Correct()
        {
            var options = _setupCampaignTestContext("Test_AddLootByTauheadURL_Correct");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.AddLootByTauheadURL(1, "Test1");
                Assert.Single(context.Item);
                Assert.Single(context.CampaignLoot);
                Assert.Single(context.Campaign.SingleOrDefault(c => c.Id == 1).Loot);
                Assert.Equal(result.Loot.Item.Slug, context.Campaign.SingleOrDefault(c => c.Id == 1).Loot.FirstOrDefault().Item.Slug);
                Assert.Equal(result.Loot.Item.Id, context.Campaign.SingleOrDefault(c => c.Id == 1).Loot.FirstOrDefault().Item.Id);
                Assert.Equal("test-item-1", result.Loot.Item.Slug);
            }
        }

        [Fact]
        public async void Test_AddLootByTauheadURL_WrongURL()
        {
            var options = _setupCampaignTestContext("Test_AddLootByTauheadURL_WrongURL");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.AddLootByTauheadURL(1, "Test2");
                Assert.Empty(context.Item);
                Assert.Empty(context.CampaignLoot);
                Assert.Null(result);
            }
        }

        [Fact]
        public async void Test_AddLootByTauheadURL_WrongCampaignId()
        {
            var options = _setupCampaignTestContext("Test_AddLootByTauheadURL_WrongCampaignId");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.AddLootByTauheadURL(2, "Test1");
                Assert.Empty(context.Item);
                Assert.Empty(context.CampaignLoot);
                Assert.Null(result);
            }
        }

        [Fact]
        public async void Test_AddLootByTauheadURL_ExistingItem()
        {
            var options = _setupCampaignTestContext("Test_AddLootByTauheadURL_ExistingItem");
            using (var context = new TauDbContext(options))
            {
                context.Item.Add(new Models.Item{
                    Name = "Test item 1",
                    Slug = "test-item-1",
                    Type = Models.Item.ItemType.Weapon,
                    WeaponType = Models.Item.ItemWeaponType.Rifle,
                    WeaponRange = Models.Item.ItemWeaponRange.Long,
                });
                context.SaveChanges();
            }

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                // This test should return and link the item already in DB - it has some different properties
                var result = await campaignLogic.AddLootByTauheadURL(1, "Test1");
                Assert.Single(context.Item);
                Assert.Single(context.CampaignLoot);
                var item = context.Item.FirstOrDefault();
                Assert.Single(context.Campaign.SingleOrDefault(c => c.Id == 1).Loot);
                Assert.Equal(result.Loot.Item.Slug, context.Campaign.SingleOrDefault(c => c.Id == 1).Loot.FirstOrDefault().Item.Slug);
                Assert.Equal(result.Loot.Item.Id, context.Campaign.SingleOrDefault(c => c.Id == 1).Loot.FirstOrDefault().Item.Id);
                Assert.Equal("test-item-1", result.Loot.Item.Slug);
                Assert.Equal(Models.Item.ItemWeaponRange.Long, item.WeaponRange); // differs from mock data
                Assert.Equal(Models.Item.ItemWeaponType.Rifle, item.WeaponType); // differs from mock data
            }
        }

        [Fact]
        public void Test_GetNewCampaign()
        {
            var options = _setupCampaignTestContext("Test_GetNewCampaign");
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = campaignLogic.GetNewCampaign(1);
                Assert.NotNull(result);
                Assert.NotNull(result.Campaign);
                Assert.Equal(Models.Campaign.CampaignDifficulty.Easy, result.Campaign.Difficulty);
                Assert.Equal(0, result.Campaign.Tiers);
                Assert.Null(result.Campaign.ManagerId);
                Assert.Equal(1, result.Campaign.SyndicateId);
            }
        }

        [Fact]
        public async void Test_ParseCampaignPage_Empty()
        {
            var options = _setupCampaignTestContext("Test_ParseCampaignPage_Empty");
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.ParseCampaignPage(String.Empty, 1);
                Assert.NotNull(result);
                var campaign = context.Campaign.FirstOrDefault();
                Assert.NotNull(campaign);
                Assert.Equal(Models.Campaign.CampaignStatus.Completed, campaign.Status);
            }
        }
        // TODO: Create a proper set of tests for parsing the different campaign files

        [Fact]
        public async void Test_SetSignupStatus_SignupCorrect()
        {
            var options = _setupCampaignTestContext("Test_SetSignupStatus_SignupCorrect");
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.SetSignupStatus(1, 1, true);
                Assert.True(result);
                Assert.Single(context.CampaignSignup);
                var campaign = context.Campaign.SingleOrDefault(c => c.Id == 1);
                Assert.Single(campaign.Signups);
                Assert.Equal(1, campaign.Signups.FirstOrDefault().PlayerId);
            }
        }

        [Fact]
        public async void Test_SetSignupStatus_SignupCorrectRemove()
        {
            var options = _setupCampaignTestContext("Test_SetSignupStatus_SignupCorrectRemove");
            using (var context = new TauDbContext(options))
            {
                context.CampaignSignup.Add(new Models.CampaignSignup{
                    CampaignId = 1,
                    Attending = true,
                    PlayerId = 1,
                });
                context.SaveChanges();
            }
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.SetSignupStatus(1, 1, false);
                Assert.True(result);
                Assert.Empty(context.CampaignSignup);
                var campaign = context.Campaign.SingleOrDefault(c => c.Id == 1);
                Assert.Empty(campaign.Signups);
            }
        }

        [Fact]
        public async void Test_SetSignupStatus_NonexistentCampaign()
        {
            var options = _setupCampaignTestContext("Test_SetSignupStatus_NonexistentCampaign");
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.SetSignupStatus(1, 2, true);
                Assert.False(result);
                Assert.Empty(context.CampaignSignup);
            }
        }

        [Fact]
        public async void Test_SetSignupStatus_NonexistentUser()
        {
            var options = _setupCampaignTestContext("Test_SetSignupStatus_NonexistentUser");
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.SetSignupStatus(5, 1, true);
                Assert.False(result);
                Assert.Empty(context.CampaignSignup);
            }
        }

        [Fact]
        public async void Test_SetSignupStatus_SignupRemoveNonexistent()
        {
            var options = _setupCampaignTestContext("Test_SetSignupStatus_SignupRemoveNonexistent");
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.SetSignupStatus(1, 1, false);
                Assert.False(result);
                Assert.Empty(context.CampaignSignup);
            }
        }

        [Fact]
        public async void Test_SetSignupStatus_SignupAddTwice()
        {
            var options = _setupCampaignTestContext("Test_SetSignupStatus_SignupAddTwice");
            using (var context = new TauDbContext(options))
            {
                context.CampaignSignup.Add(new Models.CampaignSignup{
                    CampaignId = 1,
                    Attending = true,
                    PlayerId = 1,
                });
                context.SaveChanges();
            }
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = await campaignLogic.SetSignupStatus(1, 1, true);
                Assert.False(result);
                Assert.Single(context.CampaignSignup);
            }
        }

        [Fact]
        void Test_GetCampaignAttendance_CorrectAllPlayers()
        {
            var options = _setupCampaignTestContext("Test_GetCampaignAttendance_CorrectAllPlayers");
            using (var context = new TauDbContext(options))
            {
                context.Campaign.Add(new Models.Campaign{
                    Id = 2,
                    Station = "Yards of Gadani",
                    Name = "Campaign #2",
                    Comments = "Test comments",
                    Difficulty = Models.Campaign.CampaignDifficulty.Extreme,
                    Tiers = 31,
                    SyndicateId = 1,
                    Status = Models.Campaign.CampaignStatus.Completed,
                    UTCDateTime = DateTime.Parse("2019-12-31 00:00:00"),
                    ManagerId = 1,
                });
                context.CampaignAttendance.Add(new Models.CampaignAttendance{
                    CampaignId = 1,
                    PlayerId = 1,
                });
                context.CampaignAttendance.Add(new Models.CampaignAttendance{
                    CampaignId = 2,
                    PlayerId = 1,
                });
                context.CampaignAttendance.Add(new Models.CampaignAttendance{
                    CampaignId = 2,
                    PlayerId = 3,
                });
                context.SaveChanges();
            }
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = campaignLogic.GetCampaignAttendance(null, 1);

                Assert.Equal(100, result.TotalAttendance[1]);
                Assert.Equal(100, result.T5HardAttendance[1]);
                Assert.Equal(100, result.Last10T5HardAttendance[1]);

                Assert.DoesNotContain(2, result.TotalAttendance.Keys);

                Assert.Equal(50, result.TotalAttendance[3]);
                Assert.Equal(100, result.T5HardAttendance[3]);
                Assert.Equal(100, result.Last10T5HardAttendance[3]);
            }
        }

        [Fact]
        void Test_GetCampaignAttendance_CorrectSpecificPlayer()
        {
            var options = _setupCampaignTestContext("Test_GetCampaignAttendance_CorrectSpecificPlayer");
            using (var context = new TauDbContext(options))
            {
                context.Campaign.Add(new Models.Campaign{
                    Id = 2,
                    Station = "Yards of Gadani",
                    Name = "Campaign #2",
                    Comments = "Test comments",
                    Difficulty = Models.Campaign.CampaignDifficulty.Extreme,
                    Tiers = 31,
                    SyndicateId = 1,
                    Status = Models.Campaign.CampaignStatus.Completed,
                    UTCDateTime = DateTime.Parse("2019-12-31 00:00:00"),
                    ManagerId = 1,
                });
                context.CampaignAttendance.Add(new Models.CampaignAttendance{
                    CampaignId = 1,
                    PlayerId = 1,
                });
                context.CampaignAttendance.Add(new Models.CampaignAttendance{
                    CampaignId = 2,
                    PlayerId = 1,
                });
                context.CampaignAttendance.Add(new Models.CampaignAttendance{
                    CampaignId = 2,
                    PlayerId = 3,
                });
                context.SaveChanges();
            }
            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context, _tauHeadClientMock.Object);
                var result = campaignLogic.GetCampaignAttendance(1, 1);

                Assert.Equal(100, result.TotalAttendance[1]);
                Assert.Equal(100, result.T5HardAttendance[1]);
                Assert.Equal(100, result.Last10T5HardAttendance[1]);

                Assert.DoesNotContain(2, result.TotalAttendance.Keys);
                Assert.DoesNotContain(3, result.TotalAttendance.Keys);
            }
        }
     }
}