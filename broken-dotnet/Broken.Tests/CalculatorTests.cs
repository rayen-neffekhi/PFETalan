using Xunit;
using BrokenApp;

namespace Broken.Tests
{
    public class CalculatorTests
    {
        [Fact]
        public void Add_ReturnsSum()
        {
            var sum = Calculator.Add(2, 3);
            // Intentional failing assertion (expects 6 instead of correct 5)
            Assert.Equal(6, sum);
        }

        [Fact]
        public void Divide_ByZero_Throws()
        {
            Assert.Throws<System.DivideByZeroException>(() => Calculator.Divide(10, 0));
        }
    }
}
