namespace Bfs.TestTask.Driver;

public class CardDriverTests
{
    private readonly ICardDriverMock _cardDriverMock = new CardDriverMock();

    [Fact]
    public async Task ReadCard_SetCardData_ReturnCardData()
    {
        var readCardTask = _cardDriverMock.ReadCard(CancellationToken.None);
        _cardDriverMock.SetCardData(new CardData("1234 1234 1234 1234"));

        var result = await readCardTask;

        Assert.NotNull(result);
        Assert.Equal("1234 1234 1234 1234", result.CardNumber);
    }

    [Fact]
    public async Task ReadCard_CancelTask_ReturnNull()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var readCardTask = _cardDriverMock.ReadCard(cancellationTokenSource.Token);

        await cancellationTokenSource.CancelAsync();

        var result = await readCardTask;

        Assert.Null(result);
    }

    [Fact]
    public async Task ReadCard_SetCardDataAndCancelTask_ReturnNull()
    {
        _cardDriverMock.SetCardData(new CardData("1234 1234 1234 1234"));
        using var cancellationTokenSource = new CancellationTokenSource();
        var readCardTask = _cardDriverMock.ReadCard(cancellationTokenSource.Token);

        await cancellationTokenSource.CancelAsync();

        var result = await readCardTask;

        Assert.Null(result);
    }
    
    [Fact]
    public async Task ReadCard_CantReadCard_ReturnNull()
    {
        var readCardTask = _cardDriverMock.ReadCard(CancellationToken.None);
        _cardDriverMock.CantReadCard();

        var result = await readCardTask;

        Assert.Null(result);
    }
    
    [Fact]
    public async Task ReadCard_TriggerError_ReturnNull()
    {
        var readCardTask = _cardDriverMock.ReadCard(CancellationToken.None);
        _cardDriverMock.TriggerCardReaderError();

        var result = await readCardTask;

        Assert.Null(result);
    }

    [Fact]
    public async Task ReadCard_TriggerErrorBeforeRead_ReturnNull()
    {
        _cardDriverMock.TriggerCardReaderError();
        var readCardTask = _cardDriverMock.ReadCard(CancellationToken.None);

        var result = await readCardTask;

        Assert.Null(result);
    }
    
    [Fact]
    public async Task EjectCard_TakeCard_ReturnCardTaken()
    {
        var readCardTask = _cardDriverMock.EjectCard(TimeSpan.FromSeconds(5), CancellationToken.None);

        await using var enumerator = readCardTask.GetAsyncEnumerator();
        var first = await enumerator.MoveNextAsync() ? enumerator.Current : default;

        Assert.Equal(EjectResult.Ejected, first);

        _cardDriverMock.TakeCard();

        var second = await enumerator.MoveNextAsync() ? enumerator.Current : default;

        Assert.Equal(EjectResult.CardTaken, second);
        
        var lastRead = await enumerator.MoveNextAsync();
        Assert.False(lastRead);
    }

    [Fact]
    public async Task EjectCard_TriggerErrorBeforeEject_ReturnError()
    {
        _cardDriverMock.TriggerCardReaderError();
        var readCardTask = _cardDriverMock.EjectCard(TimeSpan.FromSeconds(5), CancellationToken.None);

        await using var enumerator = readCardTask.GetAsyncEnumerator();
        var first = await enumerator.MoveNextAsync() ? enumerator.Current : default;

        Assert.Equal(EjectResult.CardReaderError, first);

        _cardDriverMock.TakeCard();

        var lastRead = await enumerator.MoveNextAsync();

        Assert.False(lastRead);
    }
    
    [Fact]
    public async Task EjectCard_TriggerErrorAfterEject_ReturnError()
    {
        var readCardTask = _cardDriverMock.EjectCard(TimeSpan.FromSeconds(5), CancellationToken.None);

        await using var enumerator = readCardTask.GetAsyncEnumerator();
        var first = await enumerator.MoveNextAsync() ? enumerator.Current : default;

        Assert.Equal(EjectResult.Ejected, first);

        _cardDriverMock.TriggerCardReaderError();

        var second = await enumerator.MoveNextAsync() ? enumerator.Current : default;
        Assert.Equal(EjectResult.CardReaderError, second);

        var lastRead = await enumerator.MoveNextAsync();

        Assert.False(lastRead);
    }
    
    [Fact]
    public async Task EjectCard_CancelTask_ReturnRetracted()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var readCardTask = _cardDriverMock.EjectCard(TimeSpan.FromSeconds(5), cancellationTokenSource.Token);

        await using var enumerator = readCardTask.GetAsyncEnumerator(CancellationToken.None);
        var first = await enumerator.MoveNextAsync() ? enumerator.Current : default;
        Assert.Equal(EjectResult.Ejected, first);

        await cancellationTokenSource.CancelAsync();

        var second = await enumerator.MoveNextAsync() ? enumerator.Current : default;
        Assert.Equal(EjectResult.Retracted, second);
        
        var lastRead = await enumerator.MoveNextAsync();
        Assert.False(lastRead);
    }
    
    [Fact]
    public async Task EjectCard_WaitCardRetractDelay_ReturnRetracted()
    {
        var currentTime = DateTime.UtcNow;
        var readCardTask = _cardDriverMock.EjectCard(TimeSpan.FromSeconds(5), CancellationToken.None);

        await using var enumerator = readCardTask.GetAsyncEnumerator();
        var first = await enumerator.MoveNextAsync() ? enumerator.Current : default;

        Assert.Equal(EjectResult.Ejected, first);
        
        var second = await enumerator.MoveNextAsync() ? enumerator.Current : default;

        Assert.Equal(EjectResult.Retracted, second);
        Assert.InRange(DateTime.UtcNow - currentTime, TimeSpan.FromMilliseconds(4900), TimeSpan.FromMilliseconds(5100));
        
        var lastRead = await enumerator.MoveNextAsync();
        Assert.False(lastRead);
    }
    
    [Fact]
    public async Task EjectCard_TakeCardBeforeEjectAndWaitCardRetractDelay_ReturnRetracted()
    {
        _cardDriverMock.TakeCard();
        
        var currentTime = DateTime.UtcNow;
        var readCardTask = _cardDriverMock.EjectCard(TimeSpan.FromSeconds(5), CancellationToken.None);

        await using var enumerator = readCardTask.GetAsyncEnumerator();
        var first = await enumerator.MoveNextAsync() ? enumerator.Current : default;

        Assert.Equal(EjectResult.Ejected, first);
        
        var second = await enumerator.MoveNextAsync() ? enumerator.Current : default;

        Assert.Equal(EjectResult.Retracted, second);
        Assert.InRange(DateTime.UtcNow - currentTime, TimeSpan.FromMilliseconds(4900), TimeSpan.FromMilliseconds(5100));
        
        var lastRead = await enumerator.MoveNextAsync();
        Assert.False(lastRead);
    }
}