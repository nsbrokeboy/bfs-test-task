using System.Threading.Channels;

namespace Bfs.TestTask.Parser;

public interface IParser
{
    //Распарсить набор сообщений из канала
    IAsyncEnumerable<IMessage> Parse(ChannelReader<ReadOnlyMemory<byte>> source);
    
    //Распарсить одно сообщение
    IMessage Parse(ReadOnlyMemory<byte> message);
}

public interface IMessage
{
}

public record CardReaderState(
    string LUNO,
    char DIG,
    int DeviceStatus,
    int ErrorSeverity,
    int DiagnosticStatus,
    int SuppliesStatus) : IMessage;

public record SendStatus(
    string LUNO,
    char StatusDescriptor,
    int TransactionNumber) : IMessage;

public record GetFitnessData(
    string LUNO,
    char StatusDescriptor,
    char MessageIdentifier,
    char HardwareFitnessIdentifier,
    FitnessState[] FitnessStates
) : IMessage;

public record FitnessState(char DIG, string Fitness);