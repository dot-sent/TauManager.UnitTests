using Xunit;
using TauManager.Utils;

namespace TauManager.UnitTests.Utils
{
    public class EnumExtensionsTests
    {
        private enum TestEnum: int { Value1, Value2, Value3, Value4, CamelCaseValue };

        [Fact]
        public void TestToDictionary_Correct()
        {
            var result = EnumExtensions.ToDictionary<int>(typeof(TestEnum));
            Assert.Equal(5, result.Keys.Count);
            Assert.Equal("Value3", result[2]);
        }

        [Fact]
        public void TestToStringSplit_Correct()
        {
            var result = TestEnum.CamelCaseValue.ToStringSplit();
            Assert.Equal("Camel Case Value", result);
        }
    }
}