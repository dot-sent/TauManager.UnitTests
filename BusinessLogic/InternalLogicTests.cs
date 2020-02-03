using Moq;
using TauManager.Utils;
using Xunit;

namespace TauManager.UnitTests.BusinessLogic
{
    public class InternalLogicTests
    {
        private Mock<ITauHeadClient> _tauHeadClientMock { get; set; }

        public InternalLogicTests()
        {

        }
        
        [Fact]
        public void Test_ImportItemsFromTauhead()
        {
        }
    }
}