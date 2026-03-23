using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Core.Validation;

public sealed class DeterministicTransactionValidator : ITransactionValidator
{
    public ValidationResult Validate(TransactionEnvelope transaction)
    {
        if (transaction.Version.Major <= 0)
        {
            return new ValidationResult(false, CoreErrorCodes.TxInvalidVersion, "Transaction version is invalid.");
        }

        if (string.IsNullOrWhiteSpace(transaction.TransactionId))
        {
            return new ValidationResult(false, CoreErrorCodes.TxMissingId, "Transaction id is required.");
        }

        if (string.IsNullOrWhiteSpace(transaction.PayloadType))
        {
            return new ValidationResult(false, CoreErrorCodes.TxMissingPayloadType, "Payload type is required.");
        }

        if (transaction.Payload is null || transaction.Payload.Length == 0)
        {
            return new ValidationResult(false, CoreErrorCodes.TxEmptyPayload, "Payload cannot be empty.");
        }

        if (transaction.Signature is null || transaction.Signature.Length == 0)
        {
            return new ValidationResult(false, CoreErrorCodes.TxMissingSignature, "Signature is required.");
        }

        return new ValidationResult(true);
    }
}
