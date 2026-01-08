using LocaGuest.Domain.Common;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Aggregates.DepositAggregate;

public class Deposit : AuditableEntity
{
    public Guid ContractId { get; private set; }

    public decimal AmountExpected { get; private set; }

    public DateTime DueDate { get; private set; }

    public bool AllowInstallments { get; private set; }

    public DepositStatus Status { get; private set; }

    private readonly List<DepositTransaction> _transactions = new();
    public IReadOnlyCollection<DepositTransaction> Transactions => _transactions.AsReadOnly();

    private Deposit() { }

    public static Deposit Create(
        Guid contractId,
        decimal amountExpected,
        DateTime dueDate,
        bool allowInstallments)
    {
        if (contractId == Guid.Empty)
            throw new ValidationException("DEPOSIT_INVALID_CONTRACT", "ContractId is required");

        if (amountExpected < 0)
            throw new ValidationException("DEPOSIT_INVALID_AMOUNT", "AmountExpected cannot be negative");

        var dueDateUtc = dueDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dueDate, DateTimeKind.Utc)
            : dueDate.ToUniversalTime();

        var deposit = new Deposit
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            AmountExpected = amountExpected,
            DueDate = dueDateUtc,
            AllowInstallments = allowInstallments,
            Status = DepositStatus.Expected
        };

        deposit.RecalculateStatus();
        return deposit;
    }

    public void RecordReceive(decimal amount, DateTime dateUtc, string? reference = null)
    {
        if (amount <= 0)
            throw new ValidationException("DEPOSIT_INVALID_RECEIVE", "Received amount must be positive");

        var currentReceived = GetTotalReceived();
        if (currentReceived + amount > AmountExpected)
            throw new ValidationException("DEPOSIT_OVER_RECEIVE", "Cannot receive more than AmountExpected");

        var tx = DepositTransaction.Create(Id, DepositTransactionKind.Receive, amount, dateUtc, reference);
        _transactions.Add(tx);

        RecalculateStatus();
    }

    public decimal GetTotalReceived() => _transactions
        .Where(t => t.Kind == DepositTransactionKind.Receive)
        .Sum(t => t.Amount);

    public decimal GetTotalRefunded() => _transactions
        .Where(t => t.Kind == DepositTransactionKind.Refund)
        .Sum(t => t.Amount);

    public decimal GetTotalDeducted() => _transactions
        .Where(t => t.Kind == DepositTransactionKind.Deduction)
        .Sum(t => t.Amount);

    public decimal GetBalanceHeld() => GetTotalReceived() - GetTotalRefunded() - GetTotalDeducted();

    public decimal GetOutstanding() => AmountExpected - GetTotalReceived();

    private void RecalculateStatus()
    {
        var received = GetTotalReceived();
        var balance = GetBalanceHeld();

        if (received == 0)
        {
            Status = DepositStatus.Expected;
            return;
        }

        if (received > 0 && received < AmountExpected)
        {
            Status = DepositStatus.PartiallyReceived;
            return;
        }

        if (received >= AmountExpected && balance > 0)
        {
            Status = DepositStatus.Held;
            return;
        }

        if (balance == 0 && received >= AmountExpected)
        {
            Status = DepositStatus.Closed;
            return;
        }

        Status = DepositStatus.Held;
    }
}

public enum DepositStatus
{
    Expected,
    PartiallyReceived,
    Held,
    Closed
}

public class DepositTransaction : Entity
{
    public Guid DepositId { get; private set; }

    public DepositTransactionKind Kind { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime DateUtc { get; private set; }

    public string? Reference { get; private set; }

    private DepositTransaction() { }

    public static DepositTransaction Create(
        Guid depositId,
        DepositTransactionKind kind,
        decimal amount,
        DateTime dateUtc,
        string? reference = null)
    {
        if (depositId == Guid.Empty)
            throw new ValidationException("DEPOSIT_TX_INVALID_DEPOSIT", "DepositId is required");

        if (amount <= 0)
            throw new ValidationException("DEPOSIT_TX_INVALID_AMOUNT", "Amount must be positive");

        var whenUtc = dateUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc)
            : dateUtc.ToUniversalTime();

        return new DepositTransaction
        {
            Id = Guid.NewGuid(),
            DepositId = depositId,
            Kind = kind,
            Amount = amount,
            DateUtc = whenUtc,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim()
        };
    }
}

public enum DepositTransactionKind
{
    Receive,
    Refund,
    Deduction,
    Adjustment
}
