using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using TauManager.BusinessLogic;
using Xunit;

namespace TauManager.UnitTests.BusinessLogic
{
    public class IntegrationLogicTests
    {
        public IntegrationLogicTests()
        {
        }

        private DbContextOptions<TauDbContext> _setupIntegrationTestContext([CallerMemberName]string databaseName = "", bool empty = false)
        {
            var options = new DbContextOptionsBuilder<TauDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;
            using (var context = new TauDbContext(options))
            {
                if (!empty)
                {
                    context.DiscordOfficer.AddRange(
                        new Models.DiscordOfficer{
                            Id = 1,
                            LoginName = "Officer1#1111"
                        },
                        new Models.DiscordOfficer{
                            Id = 2,
                            LoginName = "Officer2#2222"
                        }
                    );
                    context.SaveChanges();
                }
            }
            return options;
        }

        [Theory]
        [InlineData("OfficerAlreadyExists", "Officer1#1111", false, 2, 0)]
        [InlineData("Correct", "Officer3#3333", true, 3, 3)]
        public void Test_AddDiscordOfficer_Theory(string caption, string loginName,
            bool expectedResult, int expectedOfficerCount, int expectedNewOfficerId)
        {
            var options = _setupIntegrationTestContext("Test_AddDiscordOfficer_Theory" + caption);
            using (var context = new TauDbContext(options))
            {
                var logic = new IntegrationLogic(context);
                var result = logic.AddDiscordOfficer(loginName);
                Assert.Equal(expectedResult, result);
                var officers = context.DiscordOfficer.AsEnumerable();
                Assert.Equal(expectedOfficerCount, officers.Count());
                if (expectedResult)
                {
                    var newOfficer = officers.SingleOrDefault(o => o.Id == expectedNewOfficerId);
                    Assert.NotNull(newOfficer);
                    Assert.Equal(loginName, newOfficer.LoginName);
                }
            }
        }

        [Fact]
        public void Test_GetDiscordOfficerList_Fact()
        {
            var options = _setupIntegrationTestContext();
            using (var context = new TauDbContext(options))
            {
                var logic = new IntegrationLogic(context);
                var result = logic.GetDiscordOfficerList();
                Assert.NotNull(result);
                Assert.NotEmpty(result);
                Assert.Equal(2, result.Count);
            }
        }

        [Theory]
        [InlineData("NonExistingOfficer", "Officer55#5555", false, 2)]
        [InlineData("Correct", "Officer2#2222", true, 1)]
        public void Test_RemoveDiscordOfficer_Theory(string caption, string loginName,
            bool expectedResult, int expectedOfficerCount)
        {
            var options = _setupIntegrationTestContext("Test_RemoveDiscordOfficer_Theory" + caption);
            using (var context = new TauDbContext(options))
            {
                var logic = new IntegrationLogic(context);
                var result = logic.RemoveDiscordOfficer(loginName);
                Assert.Equal(expectedResult, result);
                var remainingOfficers = context.DiscordOfficer.AsEnumerable();
                Assert.Equal(expectedOfficerCount, remainingOfficers.Count());
                if (expectedOfficerCount == 1)
                {
                    Assert.NotEqual(loginName, remainingOfficers.First().LoginName);
                }
            }
        }
    }
}