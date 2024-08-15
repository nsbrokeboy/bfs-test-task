using System.Text;
using System.Threading.Channels;

namespace Bfs.TestTask.Parser;

public class Parser : IParser
{
    public async IAsyncEnumerable<IMessage> Parse(ChannelReader<ReadOnlyMemory<byte>> source)
    {
        //Перед каждым сообщением первые 2 байта определяют его длину
        //array[0] = (byte)(message.Length / 256);
        //array[1] = (byte)(message.Length % 256);
            
        var buffer = Array.Empty<byte>();
        var bufferLength = 0;

        while (await source.WaitToReadAsync())
        {
            while (source.TryRead(out var memory))
            {
                // Увеличиваем буфер, если нужно
                if (bufferLength + memory.Length > buffer.Length)
                {
                    Array.Resize(ref buffer, bufferLength + memory.Length);
                }

                // Копируем новые данные в буфер
                memory.Span.CopyTo(buffer.AsSpan(bufferLength));
                bufferLength += memory.Length;

                // Обработка сообщений в буфере
                while (bufferLength >= 2)
                {
                    // Получаем длину сообщения из первых двух байтов
                    var messageLength = (buffer[0] << 8) | buffer[1];

                    // Если в буфере меньше данных, чем длина сообщения, ждем получения остальной части
                    if (bufferLength < messageLength + 2)
                    {
                        break;
                    }

                    // Парсим сообщение
                    var message = Parse(new ReadOnlyMemory<byte>(buffer, 2, messageLength));
                    yield return message;

                    // Сдвигаем буфер, удаляя обработанное сообщение
                    bufferLength -= (messageLength + 2);
                    if (bufferLength > 0)
                    {
                        Array.Copy(buffer, messageLength + 2, buffer, 0, bufferLength);
                    }
                }
            }
        }
    }

    public IMessage Parse(ReadOnlyMemory<byte> message)
    {
        // Преобразуем байты в строку, только когда это необходимо
        var messageString = Encoding.ASCII.GetString(message.Span);

        if (messageString.StartsWith("12"))
        {
            return ParseCardReaderState(messageString);
        }

        if (messageString.StartsWith("22") && messageString.Contains("B"))
        {
            return ParseSendStatus(messageString);
        }

        if (messageString.StartsWith("22") && messageString.Contains("F"))
        {
            return ParseGetFitnessData(messageString);
        }

        throw new InvalidOperationException("Неизвестный формат сообщения");
    }

    private CardReaderState ParseCardReaderState(string message)
    {
        var parts = message.Split('');
        var luno = parts[1];
        var dig = parts[3][0];
        var deviceStatus = parts[3][1] - '0';
        var errorSeverity = parts[3][2] - '0';
        var diagnosticStatus = parts[3][3] - '0';
        var suppliesStatus = parts[3][4] - '0';

        return new CardReaderState(luno, dig, deviceStatus, errorSeverity, diagnosticStatus, suppliesStatus);
    }

    private SendStatus ParseSendStatus(string message)
    {
        var parts = message.Split('');
        var luno = parts[1];
        var statusDescriptor = parts[3][0];
        var transactionNumber = int.Parse(parts[5]);

        return new SendStatus(luno, statusDescriptor, transactionNumber);
    }

    private GetFitnessData ParseGetFitnessData(string message)
    {
        var parts = message.Split('');
        var luno = parts[1];
        var statusDescriptor = parts[3][0];
        var messageIdentifier = parts[4][0];
        var hardwareFitnessIdentifier = parts[4][1];

        var fitnessParts = parts[4].Substring(2).Split('');
        var fitnessStates = new FitnessState[fitnessParts.Length];

        for (var i = 0; i < fitnessParts.Length; i++)
        {
            var dig = fitnessParts[i][0];
            var fitness = fitnessParts[i].Substring(1);
            fitnessStates[i] = new FitnessState(dig, fitness);
        }

        return new GetFitnessData(luno, statusDescriptor, messageIdentifier, hardwareFitnessIdentifier, fitnessStates);
    }
}

/*
   Message with type Card Reader State builded:
   1200100355D1001
   Description:
   (b) 1 = Message class
   (c) 2 = Message sub-class
   (d) 00100355 = LUNO
   CardReaderStateDto { Solicited = False, DeviceIdCode = D, SupplyState = NoOverfillCondition, Status = TimeOutCardHolderTakingCard, Severity = NoError }
   (g1) D = Device Identifier Graphic
   (g2) 1 = Device Status (TimeOutCardHolderTakingCard)
   (g3) 0 = Error Severity (NoError)
   (g4) 0 = Diagnostic Status
   (g5) 1 = Supplies Status (NoOverfillCondition)


   Message with type Send Status builded:
   2200100355B4321
   Description:
   (b) 2 = Message class
   (c) 2 = Message sub-class
   (d) 00100355 = LUNO
   Status data
   (f) B = Status Descriptor (TransactionReplyReady)
   Status Information
   (g1) 4321 = Transaction number


   Message with type Get Fitness Data builded:
   2200100355FJAD01y1A0E00000G0L0w00040003000200010H0
   Description:
   (b) 2 = Message class
   (c) 2 = Message sub-class
   (d) 00100355 = LUNO
   MagneticCardReader RoutineErrorsHaveOccurred,SecondaryCardReader RoutineErrorsHaveOccurred,TimeOfDayClock NoError,CashHandler NoError,ReceiptPrinter NoError,Encryptor NoError,BunchNoteAcceptor NoError,JournalPrinter NoError
   (f) F = Status Descriptor (TerminalState)
   Status Information
   (g1) J = Message Identifier (FitnessData)
   (g2) A = Hardware Fitness Identifier
   (g2) D = Device Identifier Graphic MagneticCardReader
   (g2) 01 = Fitness - RoutineErrorsHaveOccurred
   (g2) y = Device Identifier Graphic SecondaryCardReader
   (g2) 1 = Fitness - RoutineErrorsHaveOccurred
   (g2) A = Device Identifier Graphic TimeOfDayClock
   (g2) 0 = Fitness - NoError
   (g2) E = Device Identifier Graphic CashHandler
   (g2) 00000 = Fitness - NoError
   (g2) G = Device Identifier Graphic ReceiptPrinter
   (g2) 0 = Fitness - NoError
   (g2) L = Device Identifier Graphic Encryptor
   (g2) 0 = Fitness - NoError
   (g2) w = Device Identifier Graphic BunchNoteAcceptor
   (g2) 00040003000200010 = Fitness - NoError
   (g2) H = Device Identifier Graphic JournalPrinter
   (g2) 0 = Fitness - NoError

 */