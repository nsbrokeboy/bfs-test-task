namespace Bfs.TestTask.Driver;

public interface ICardDriver
{
    Task<CardData?> ReadCard(CancellationToken cancellationToken);
    
    IAsyncEnumerable<EjectResult> EjectCard(TimeSpan takeCardTimeout, CancellationToken cancellationToken);
}

public interface ICardDriverMock : ICardDriver
{
    void SetCardData(CardData cardData);
    void CantReadCard();
    void TakeCard();
    void TriggerCardReaderError();    
}

public enum EjectResult
{
    Ejected,
    CardTaken,
    Retracted,
    CardReaderError
}

public record CardData(string CardNumber);