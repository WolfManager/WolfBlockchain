using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Core.Validation;

public sealed class DeterministicBlockValidator(ITransactionValidator transactionValidator) : IBlockValidator
{
    public ValidationResult Validate(BlockEnvelope block)
    {
        if (block.Version.Major <= 0)
        {
            return new ValidationResult(false, CoreErrorCodes.BlockInvalidVersion, "Block version is invalid.");
        }

        if (block.Height < 0)
        {
            return new ValidationResult(false, CoreErrorCodes.BlockInvalidHeight, "Block height must be non-negative.");
        }

        if (string.IsNullOrWhiteSpace(block.BlockHash))
        {
            return new ValidationResult(false, CoreErrorCodes.BlockMissingHash, "Block hash is required.");
        }

        if (block.Height > 0 && string.IsNullOrWhiteSpace(block.PreviousBlockHash))
        {
            return new ValidationResult(false, CoreErrorCodes.BlockMissingPreviousHash, "Previous block hash is required for non-genesis blocks.");
        }

        if (block.Transactions is null)
        {
            return new ValidationResult(false, CoreErrorCodes.BlockMissingTransactions, "Transactions collection is required.");
        }

        var seenTransactionIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var transaction in block.Transactions)
        {
            var transactionValidation = transactionValidator.Validate(transaction);
            if (!transactionValidation.IsValid)
            {
                return new ValidationResult(false, CoreErrorCodes.BlockInvalidTransaction, transactionValidation.ErrorMessage);
            }

            if (!seenTransactionIds.Add(transaction.TransactionId))
            {
                return new ValidationResult(false, CoreErrorCodes.BlockDuplicateTransaction, "Duplicate transaction id in block.");
            }
        }

        return new ValidationResult(true);
    }
}
