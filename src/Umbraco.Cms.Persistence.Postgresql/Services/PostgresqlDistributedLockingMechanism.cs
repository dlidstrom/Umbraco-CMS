using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DistributedLocking;
using Umbraco.Cms.Core.DistributedLocking.Exceptions;
using Umbraco.Cms.Core.Exceptions;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Umbraco.Cms.Persistence.Postgresql.Services;

public class PostgresqlDistributedLockingMechanism : IDistributedLockingMechanism
{
    private readonly IOptionsMonitor<ConnectionStrings> _connectionStrings;
    private readonly IOptionsMonitor<GlobalSettings> _globalSettings;
    private readonly ILogger _logger;
    private readonly Lazy<IScopeAccessor> _scopeAccessor;

    public PostgresqlDistributedLockingMechanism(
        ILogger<PostgresqlDistributedLockingMechanism> logger,
        Lazy<IScopeAccessor> scopeAccessor,
        IOptionsMonitor<GlobalSettings> globalSettings,
        IOptionsMonitor<ConnectionStrings> connectionStrings)
    {
        _logger = logger;
        _scopeAccessor = scopeAccessor;
        _connectionStrings = connectionStrings;
        _globalSettings = globalSettings;
    }

    /// <inheritdoc />
    public bool Enabled => _connectionStrings.CurrentValue.IsConnectionStringConfigured() &&
                           string.Equals(_connectionStrings.CurrentValue.ProviderName, Constants.ProviderName, StringComparison.InvariantCultureIgnoreCase);

    /// <inheritdoc />
    public IDistributedLock ReadLock(int lockId, TimeSpan? obtainLockTimeout = null)
    {
        obtainLockTimeout ??= _globalSettings.CurrentValue.DistributedLockingReadLockDefaultTimeout;
        return new PostgresqlDistributedLock(this, lockId, DistributedLockType.ReadLock, obtainLockTimeout.Value);
    }

    /// <inheritdoc />
    public IDistributedLock WriteLock(int lockId, TimeSpan? obtainLockTimeout = null)
    {
        // With journal_mode=wal only a single write transaction can exist at a time.
        obtainLockTimeout ??= _globalSettings.CurrentValue.DistributedLockingWriteLockDefaultTimeout;
        return new PostgresqlDistributedLock(this, lockId, DistributedLockType.WriteLock, obtainLockTimeout.Value);
    }

    private class PostgresqlDistributedLock : IDistributedLock
    {
        private readonly PostgresqlDistributedLockingMechanism _parent;
        private readonly TimeSpan _timeout;

        public PostgresqlDistributedLock(
            PostgresqlDistributedLockingMechanism parent,
            int lockId,
            DistributedLockType lockType,
            TimeSpan timeout)
        {
            _parent = parent;
            _timeout = timeout;
            LockId = lockId;
            LockType = lockType;

            _parent._logger.LogDebug("Requesting {lockType} for id {id}", LockType, LockId);

            try
            {
                switch (lockType)
                {
                    case DistributedLockType.ReadLock:
                        ObtainReadLock();
                        break;
                    case DistributedLockType.WriteLock:
                        ObtainWriteLock();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(lockType), lockType, @"Unsupported lockType");
                }
            }
            catch (NpgsqlException)
            {
                if (LockType == DistributedLockType.ReadLock)
                {
                    throw new DistributedReadLockTimeoutException(LockId);
                }

                throw new DistributedWriteLockTimeoutException(LockId);
            }

            _parent._logger.LogDebug("Acquired {lockType} for id {id}", LockType, LockId);
        }

        public int LockId { get; }

        public DistributedLockType LockType { get; }

        // Mostly no op, cleaned up by completing transaction in scope.
        public void Dispose() =>
            _parent._logger.LogDebug("Dropped {lockType} for id {id}", LockType, LockId);

        public override string ToString()
            => $"PostgresqlDistributedLock({LockId})";

        private void ObtainReadLock()
        {
            IUmbracoDatabase? db = _parent._scopeAccessor.Value.AmbientScope?.Database;

            if (db is null)
            {
                throw new PanicException("no database was found");
            }

            if (!db.InTransaction)
            {
                throw new InvalidOperationException(
                    "PostgresqlDistributedLockingMechanism requires a transaction to function.");
            }
        }

        private void ObtainWriteLock()
        {
            IUmbracoDatabase? db = _parent._scopeAccessor.Value.AmbientScope?.Database;

            if (db is null)
            {
                throw new PanicException("no database was found");
            }

            if (!db.InTransaction)
            {
                throw new InvalidOperationException(
                    "PostgresqlDistributedLockingMechanism requires a transaction to function.");
            }

            var query = @$"UPDATE umbracoLock SET value = (CASE WHEN (value=1) THEN -1 ELSE 1 END) WHERE id = @LockId";
            DbCommand command = db.CreateCommand(db.Connection, CommandType.Text, query);
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = "@LockId";
            parameter.Value = LockId;
            _ = command.Parameters.Add(parameter);

            // imagine there is an existing writer, whilst elapsed time is < command timeout sqlite will busy loop
            // Important to note that if this value == 0 then Command.DefaultTimeout (30s) is used.
            // Math.Ceiling such that (0 < totalseconds < 1) is rounded up to 1.
            command.CommandTimeout = (int)Math.Ceiling(_timeout.TotalSeconds);

            try
            {
                var i = command.ExecuteNonQuery();

                if (i == 0)
                {
                    // ensure we are actually locking!
                    throw new ArgumentException($"LockObject with id={LockId} does not exist.");
                }
            }
            catch (NpgsqlException)
            {
                throw new DistributedWriteLockTimeoutException(LockId);
            }
        }
    }
}
