using System.Runtime.CompilerServices;

namespace Bfs.TestTask.Driver
{
    public enum CardState
    {
        NoCard,
        CardInserted,
        CardEjected,
        CardTaken,
        CardReaderError,
        CantReadCard
    }

    public class CardDriverMock : ICardDriverMock
    {
        private CardData? _cardData;
        private CardState _cardState = CardState.NoCard;

        public async Task<CardData?> ReadCard(CancellationToken cancellationToken)
        {
            if (_cardState is CardState.CardReaderError or CardState.CantReadCard)
            {
                return null;
            }

            // имитация задержки для чтения карты. специально не передаем cancellation token, т.к. метод Delay бросит исключение об отмене 
            await Task.Delay(100);
            if (cancellationToken.IsCancellationRequested || _cardState == CardState.NoCard)
            {
                return null;
            }

            return _cardData;
        }

        public async IAsyncEnumerable<EjectResult> EjectCard(TimeSpan takeCardTimeout, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_cardState == CardState.CardReaderError)
            {
                yield return EjectResult.CardReaderError;
                yield break;
            }

            // имитация выброса карты
            _cardState = CardState.CardEjected;
            yield return EjectResult.Ejected;

            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < takeCardTimeout)
            {
                if (_cardState == CardState.CardReaderError)
                {
                    yield return EjectResult.CardReaderError;
                    yield break;
                }

                if (_cardState == CardState.CardTaken)
                {
                    yield return EjectResult.CardTaken;
                    yield break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _cardState = CardState.NoCard;
                    yield return EjectResult.Retracted;
                    yield break;
                }

                await Task.Delay(100, cancellationToken);
            }

            // если карта не была взята, убираем её обратно
            _cardState = CardState.NoCard;
            yield return EjectResult.Retracted;
        }

        public void SetCardData(CardData cardData)
        {
            _cardData = cardData;
            _cardState = CardState.CardInserted;
        }

        public void CantReadCard()
        {
            _cardState = CardState.CantReadCard;
        }

        public void TakeCard()
        {
            if (_cardState == CardState.CardEjected)
            {
                _cardState = CardState.CardTaken;
            }
        }

        public void TriggerCardReaderError()
        {
            _cardState = CardState.CardReaderError;
        }
    }
}