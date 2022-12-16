using Xunit.Abstractions;

namespace TTSS.TestHelpers.XUnit
{
    public abstract class XUnitTestBase
    {
        private readonly ITestOutputHelper testOutput;

        public XUnitTestBase(ITestOutputHelper testOutput)
            => this.testOutput = testOutput;

        public void WriteLine(string message)
            => testOutput.WriteLine(message);

        public void WriteLine(string format, params object[] args)
            => testOutput.WriteLine(format, args);
    }
}