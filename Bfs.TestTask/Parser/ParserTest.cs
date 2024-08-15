using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace Bfs.TestTask.Parser;

public class ParserTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ParserTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ParseMessages()
    {
        var cardReaderState = new CardReaderState("00100355", 'D', 1, 0, 0, 1);
        var sendStatus = new SendStatus("00100355", 'B', 4321);
        var getFitnessData = new GetFitnessData("00100355", 'F', 'J', 'A', new FitnessState[]
        {
            new('D', "01"),
            new('y', "1"),
            new('A', "0"),
            new('E', "00000"),
            new('G', "0"),
            new('L', "0"),
            new('w', "00040003000200010"),
            new('H', "0")
        });

        var otherFitnessData = new GetFitnessData("355", 'F', 'J', 'A', new FitnessState[]
        {
            new('D', "01"),
            new('y', "1"),
            new('A', "0"),
            new('w', "0004000310"),
            new('H', "0")
        });

        var source = new MessageSource();

        var channel = source.Reader;

        var parser = new Parser();

        _ = source.StartConsume();

        int index = 0;

        await foreach (var message in parser.Parse(channel))
        {
            if (message is CardReaderState cardReaderStateMessage)
            {
                Assert.Equal(0, index);
                Assert.Equal(cardReaderState, cardReaderStateMessage);
            }
            else if (message is SendStatus sendStatusMessage)
            {
                Assert.Equal(1, index);
                Assert.Equal(sendStatus, sendStatusMessage);
            }
            else if (message is GetFitnessData getFitnessDataMessage)
            {
                if (index == 2)
                {
                    Assert.Equivalent(getFitnessData, getFitnessDataMessage, true);
                }
                else if (index == 3)
                {
                    Assert.Equivalent(otherFitnessData, getFitnessDataMessage, true);
                }
                else
                {
                    Assert.Fail();
                }
            }

            index++;
        }

        Assert.Equal(4, index);
    }

    [Fact]
    public void StressTest()
    {
        var parser = new Parser();

        var sendStatus = @"2200100355B";

        byte[][] read = new byte[10_000_000][];
        for (int i = 0; i < read.Length; i++)
        {
            read[i] = Encoding.ASCII.GetBytes(sendStatus + i);
        }

        Stopwatch stopwatch = new Stopwatch();
        var startAllocatedBytes = GC.GetTotalAllocatedBytes(true);
        stopwatch.Start();

        IMessage message = null!;

        for (int i = 0; i < read.Length; i++)
        {
            message = parser.Parse(read[i]);
        }

        Assert.Equal(((SendStatus)message).TransactionNumber, read.Length - 1);

        stopwatch.Stop();
        var stopAllocatedBytes = GC.GetTotalAllocatedBytes(true);
        var totalAllocatedBytes = stopAllocatedBytes - startAllocatedBytes;

        _testOutputHelper.WriteLine($"Total allocated bytes: {totalAllocatedBytes}");
        _testOutputHelper.WriteLine($"Total allocated MBytes: {totalAllocatedBytes / 1024 / 1024}");

        _testOutputHelper.WriteLine(
            $"Best allocations result bytes: {32 * (long)10_000_000}"); // На ARM64 объект занимает 32 байта

        _testOutputHelper.WriteLine(
            $"Best allocations result MBytes: {32 * (long)10_000_000 / 1024 / 1024}");

        _testOutputHelper.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}");

        // Total allocated bytes: 320078880
        // Total allocated MBytes: 305
        // Best allocations result bytes: 320000000
        // Best allocations result MBytes: 305
        // Total time: 1863
    }
}