using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Nyx.Cli.Tests;

class TestOutputHelperTextWriter : TextWriter
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly StringBuilder _buffer = new();

    public TestOutputHelperTextWriter(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
        
    public override Encoding Encoding { get; } = Encoding.UTF8;

    public override void Write(char value)
    {
        switch (value)
        {
            case '\r':
                break;
            case '\n':
                _testOutputHelper.WriteLine(_buffer.ToString());
                _buffer.Clear();
                break;
            default:
                _buffer.Append(value);
                break;
        }
    }
}