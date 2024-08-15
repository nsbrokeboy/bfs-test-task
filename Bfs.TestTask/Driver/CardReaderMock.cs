namespace Bfs.TestTask.Driver;

public class CardDriverMock : ICardDriverMock
{
    public Task<CardData?> ReadCard(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public IAsyncEnumerable<EjectResult> EjectCard(TimeSpan takeCardTimeout, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    
    public void SetCardData(CardData cardData)
    {
        throw new NotImplementedException();
    }

    public void CantReadCard()
    {
        throw new NotImplementedException();
    }

    public void TakeCard()
    {
        throw new NotImplementedException();
    }

    public void TriggerCardReaderError()
    {
        throw new NotImplementedException();
    }
}