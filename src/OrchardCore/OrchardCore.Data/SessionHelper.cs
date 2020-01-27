using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql;

namespace OrchardCore.Data
{
    /// <summary>
    /// Represents a class that provides helper methods for the <see cref="ISession"/>.
    /// </summary>
    public class SessionHelper : ISessionHelper
    {
        private readonly ISession _session;

        private readonly Dictionary<Type, object> _loaded = new Dictionary<Type, object>();

        private readonly List<Type> _beforeCommits = new List<Type>();
        private readonly List<Type> _afterCommitsSuccess = new List<Type>();
        private readonly List<Type> _afterCommits = new List<Type>();

        private CommitDelegate _beforeCommit;
        private CommitDelegate _afterCommitSuccess;
        private CommitDelegate _afterCommit;

        /// <summary>
        /// Creates a new instance of <see cref="SessionHelper"/>.
        /// </summary>
        public SessionHelper(ISession session)
        {
            _session = session;
        }

        /// <inheritdoc />
        public async Task<T> LoadForUpdateAsync<T>(Func<T> factory = null) where T : class, new()
        {
            if (_loaded.TryGetValue(typeof(T), out var loaded))
            {
                return loaded as T;
            }

            var document = await _session.Query<T>().FirstOrDefaultAsync() ?? factory?.Invoke() ?? new T();

            _loaded[typeof(T)] = document;

            return document;
        }

        /// <inheritdoc />
        public async Task<T> GetForCachingAsync<T>(Func<T> factory = null) where T : class, new()
        {
            if (_loaded.TryGetValue(typeof(T), out var loaded))
            {
                _session.Detach(loaded);
            }

            var document = await _session.Query<T>().FirstOrDefaultAsync();

            if (document != null)
            {
                _session.Detach(document);
                return document;
            }

            return factory?.Invoke() ?? new T();
        }

        /// <inheritdoc />
        public void RegisterBeforeCommit<T>(CommitDelegate beforeCommit)
        {
            if (!_beforeCommits.Contains(typeof(T)))
            {
                _beforeCommits.Add(typeof(T));
                _beforeCommit += beforeCommit;
            }
        }

        /// <inheritdoc />
        public void RegisterAfterCommitSuccess<T>(CommitDelegate afterCommit)
        {
            if (!_afterCommitsSuccess.Contains(typeof(T)))
            {
                _afterCommitsSuccess.Add(typeof(T));
                _afterCommitSuccess += afterCommit;
            }
        }

        /// <inheritdoc />
        public void RegisterAfterCommit<T>(CommitDelegate afterCommit)
        {
            if (!_afterCommits.Contains(typeof(T)))
            {
                _afterCommits.Add(typeof(T));
                _afterCommit += afterCommit;
            }
        }

        /// <inheritdoc />
        public async Task CommitAsync()
        {
            try
            {
                if (_beforeCommit != null)
                {
                    await _beforeCommit();
                }

                await _session.CommitAsync();

                if (_afterCommitSuccess != null)
                {
                    await _afterCommitSuccess();
                }
            }
            catch
            {
            }
            finally
            {
                if (_afterCommit != null)
                {
                    await _afterCommit();
                }
            }
        }
    }
}
