using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using TauManager.BusinessLogic;
using TauManager.Utils;
using Xunit;

namespace TauManager.UnitTests.BusinessLogic
{
    public class InternalLogicTests
    {
        private Mock<ITauHeadClient> _tauHeadClientMock { get; set; }


        public InternalLogicTests()
        {
            _tauHeadClientMock = new Mock<ITauHeadClient>();
            _tauHeadClientMock.Setup(th => th.BulkParseItems("TestSet1")).Returns(
                new Dictionary<String, Models.Item>{
                    {
                        "item1",
                        new Models.Item{
                            Slug = "item1",
                            Name = "Test1",
                            Tier = 1,
                            Type = Models.Item.ItemType.Weapon,
                            Rarity = Models.Item.ItemRarity.Epic,
                        }
                    },
                    {
                        "item2",
                        new Models.Item
                        {
                            Slug = "item2",
                            Name = "Test2",
                            Tier = 2,
                            Type = Models.Item.ItemType.Weapon,
                            Rarity = Models.Item.ItemRarity.Epic,
                        }
                    },
                    {
                        "item3",
                        new Models.Item
                        {
                            Slug = "item3",
                            Name = "Test3",
                            Tier = 2,
                            Type = Models.Item.ItemType.Armor,
                            Rarity = Models.Item.ItemRarity.Epic,
                        }
                    },
                    {
                        "item4",
                        null
                    }
                }
            );
        }
        
        [Fact]
        public async void Test_ImportItemsFromTauheadFact()
        {
            var options = new DbContextOptionsBuilder<TauDbContext>()
                .UseInMemoryDatabase(databaseName: "Test_ImportItemsFromTauheadFact")
                .Options;
            using (var context = new TauDbContext(options))
            {
                context.Item.Add(new Models.Item
                {
                    Slug = "item2",
                    Name = "Test2",
                    Tier = 2,
                    Type = Models.Item.ItemType.Weapon,
                    Rarity = Models.Item.ItemRarity.Rare,
                });
                context.SaveChanges();
            }
            using (var context = new TauDbContext(options))
            {
                var internalLogic = new InternalLogic(context, _tauHeadClientMock.Object);
                var result = await internalLogic.ImportItemsFromTauhead("TestSet1");
                var items = context.Item.AsEnumerable();
                Assert.Equal(3, items.Count());
                var updatedItem = items.SingleOrDefault(i => i.Slug == "item2");
                Assert.NotNull(updatedItem);
                Assert.Equal(Models.Item.ItemRarity.Epic, updatedItem.Rarity);
            }
        }
    }
}