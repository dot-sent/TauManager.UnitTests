using Xunit;
using TauManager.BusinessLogic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace TauManager.UnitTests.BusinessLogic
{
    public class CampaignLogicTests
    {
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
                var campaignLogic = new CampaignLogic(context);
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
                var campaignLogic = new CampaignLogic(context);
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
                var campaignLogic = new CampaignLogic(context);
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
                var campaignLogic = new CampaignLogic(context);
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
                var campaignLogic = new CampaignLogic(context);
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
        public async System.Threading.Tasks.Task Test_CreateOrEditCampaign_EditNonExistentCampaignAsync()
        {
            var options = _setupCampaignTestContext("Test_CreateOrEditCampaign_EditNonExistentCampaignAsync");

            using (var context = new TauDbContext(options))
            {
                var campaignLogic = new CampaignLogic(context);
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
     }
}