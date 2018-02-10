using System;
using System.Transactions;

namespace Gust
{
    public class TransactionSettings
    {
        /// <summary>
        /// Create a TransactionSettings object using default settings.
        /// </summary>
        /// <remarks>
        /// Defaults the <see cref="IsolationLevel"/> to ReadCommitted, 
        /// the <see cref="Timeout"/> to TransactionManager.DefaultTimeout (which is usually 1 minute),
        /// and <see cref="TransactionType"/> to TransactionType.None (which means the other settings have no effect).  
        /// These settings are not Breeze requirements and can be changed using the appropriate constructor or setter.
        /// </remarks>
        public TransactionSettings()
        {
            IsolationLevel = IsolationLevel.ReadCommitted;
            Timeout = TransactionManager.DefaultTimeout;
            TransactionType = TransactionType.None;
        }

        /// <summary>
        /// Create a TransactionSettings object with the specified settings.
        /// </summary>
        /// <remarks>
        /// Note that IsolationLevel and Timeout have no affect if TransactionType is None.
        /// </remarks>
        public TransactionSettings(IsolationLevel isolationLevel, TimeSpan timeout, TransactionType transactionType)
        {
            IsolationLevel = isolationLevel;
            Timeout = timeout;
            TransactionType = transactionType;
        }

        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// Gets the transaction locking behavior.
        /// </summary>
        /// <remarks>
        /// Only applicable if <see cref="TransactionType"/> is not <code>None</code>.  The default IsolationLevel is ReadCommitted.
        /// </remarks>
        public IsolationLevel IsolationLevel { get; set; }

        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Converts the TransactionSettings to a <see cref="System.Transactions.TransactionOptions" /> instance.
        /// </summary>
        /// <returns></returns>
        public TransactionOptions ToTransactionOptions()
        {
            var options = new TransactionOptions
            {
                IsolationLevel = IsolationLevel,
                Timeout = Timeout
            };

            return options;
        }
    }

    /// <summary><list>
    ///  DbTransaction - Use the transaction from the DbConnection.  Only works against the single connection.
    ///  None - BeforeSaveEntity/ies, SaveChangesCore, and AfterSaveEntities are not executed in the same transaction.
    /// </list></summary>
    public enum TransactionType
    {
        DbTransaction,
        None
    }
}
