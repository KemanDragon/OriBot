using System;
using System.Collections.Generic;
using System.Linq;

namespace OriBot.Transactions
{

    public sealed class Transaction
    {
        private DateTime StartedTime = DateTime.UtcNow;
        private DateTime _MaxConfirmTime = DateTime.UtcNow;

        private TransactionData transactionData;
        public string GUID = Guid.NewGuid().ToString();

        public DateTime MaxConfirmTime { get => _MaxConfirmTime; }
        public TransactionData TransactionData { get => transactionData; }

        public Transaction(DateTime maxconfirm, TransactionData transactionData2)
        {
            _MaxConfirmTime = maxconfirm;
            transactionData = transactionData2;
        }
    }

    public abstract class TransactionData
    {

    }

    public class TransactionContainer
    {
        private List<Transaction> transactions = new List<Transaction>();

        public string StartTransaction(DateTime maxconfirm, TransactionData transactionData)
        {
            var transaction = new Transaction(maxconfirm, transactionData);
            transactions.Add(transaction);
            return transaction.GUID;
        }

        public bool CheckTransaction(string guid, bool remove = true)
        {
            foreach (var transaction in transactions.Where(x => x.GUID == guid))
            {

                if (DateTime.UtcNow > transaction.MaxConfirmTime)
                {
                    if (remove) transactions.Remove(transaction);
                    return false;

                }
                else
                {
                    if (remove) transactions.Remove(transaction);
                    return true;
                }
            }
            return false;
        }

        public Transaction GetTransactionById(string id, bool remove = true) {
            foreach (var transaction in transactions.Where(x => x.GUID == id))
            {
                if (remove) transactions.Remove(transaction);
                return transaction;
            }
            return null;
        }

        public bool CancelTransaction(string guid)
        {
            foreach (var transaction in transactions.Where(x => x.GUID == guid))
            {
                transactions.Remove(transaction);
                return true;
            }
            return false;
        }
    }
}