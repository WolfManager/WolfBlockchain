using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Core.Mempool;

public sealed class SafeMempoolService(ITransactionValidator transactionValidator, int maxSize = 10_000) : IMempoolService
{
    private readonly object _sync = new();
    private readonly Dictionary<string, TransactionEnvelope> _pendingById = new(StringComparer.Ordinal);

    public ValidationResult TryAcceptTransaction(TransactionEnvelope transaction)
    {
        var validation = transactionValidator.Validate(transaction);
        if (!validation.IsValid)
        {
            return validation;
        }

        lock (_sync)
        {
            if (_pendingById.ContainsKey(transaction.TransactionId))
            {
                return new ValidationResult(false, CoreErrorCodes.MempoolDuplicateTransaction, "Transaction already exists in mempool.");
            }

            if (_pendingById.Count >= maxSize)
            {
                return new ValidationResult(false, CoreErrorCodes.MempoolFull, "Mempool capacity reached.");
            }

            _pendingById[transaction.TransactionId] = transaction;
            return new ValidationResult(true);
        }
    }

    public IReadOnlyList<TransactionEnvelope> GetPendingTransactions(int limit)
    {
        if (limit <= 0)
        {
            return Array.Empty<TransactionEnvelope>();
        }

        lock (_sync)
        {
            return _pendingById.Values
                .OrderBy(tx => tx.Payload.Length)
                .ThenBy(tx => tx.TransactionId, StringComparer.Ordinal)
                .Take(limit)
                .ToArray();
        }
    }
}
