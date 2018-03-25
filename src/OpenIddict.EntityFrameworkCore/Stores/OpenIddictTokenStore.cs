﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Core;
using OpenIddict.Models;
using OpenIddict.Stores;

namespace OpenIddict.EntityFrameworkCore
{
    /// <summary>
    /// Provides methods allowing to manage the tokens stored in a database.
    /// Note: this class can only be used with the default OpenIddict entities.
    /// </summary>
    /// <typeparam name="TContext">The type of the Entity Framework database context.</typeparam>
    public class OpenIddictTokenStore<TContext> : OpenIddictTokenStore<OpenIddictToken,
                                                                       OpenIddictApplication,
                                                                       OpenIddictAuthorization, TContext, string>
        where TContext : DbContext
    {
        public OpenIddictTokenStore([NotNull] TContext context, [NotNull] IMemoryCache cache)
            : base(context, cache)
        {
        }
    }

    /// <summary>
    /// Provides methods allowing to manage the tokens stored in a database.
    /// Note: this class can only be used with the default OpenIddict entities.
    /// </summary>
    /// <typeparam name="TContext">The type of the Entity Framework database context.</typeparam>
    /// <typeparam name="TKey">The type of the entity primary keys.</typeparam>
    public class OpenIddictTokenStore<TContext, TKey> : OpenIddictTokenStore<OpenIddictToken<TKey>,
                                                                             OpenIddictApplication<TKey>,
                                                                             OpenIddictAuthorization<TKey>, TContext, TKey>
        where TContext : DbContext
        where TKey : IEquatable<TKey>
    {
        public OpenIddictTokenStore([NotNull] TContext context, [NotNull] IMemoryCache cache)
            : base(context, cache)
        {
        }
    }

    /// <summary>
    /// Provides methods allowing to manage the tokens stored in a database.
    /// Note: this class can only be used with the default OpenIddict entities.
    /// </summary>
    /// <typeparam name="TToken">The type of the Token entity.</typeparam>
    /// <typeparam name="TApplication">The type of the Application entity.</typeparam>
    /// <typeparam name="TAuthorization">The type of the Authorization entity.</typeparam>
    /// <typeparam name="TContext">The type of the Entity Framework database context.</typeparam>
    /// <typeparam name="TKey">The type of the entity primary keys.</typeparam>
    public class OpenIddictTokenStore<TToken, TApplication, TAuthorization, TContext, TKey> :
        OpenIddictTokenStore<TToken, TApplication, TAuthorization, TKey>
        where TToken : OpenIddictToken<TKey, TApplication, TAuthorization>, new()
        where TApplication : OpenIddictApplication<TKey, TAuthorization, TToken>, new()
        where TAuthorization : OpenIddictAuthorization<TKey, TApplication, TToken>, new()
        where TContext : DbContext
        where TKey : IEquatable<TKey>
    {
        public OpenIddictTokenStore([NotNull] TContext context, [NotNull] IMemoryCache cache)
            : base(cache)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Context = context;
        }

        /// <summary>
        /// Gets the database context associated with the current store.
        /// </summary>
        protected virtual TContext Context { get; }

        /// <summary>
        /// Gets the database set corresponding to the <typeparamref name="TApplication"/> entity.
        /// </summary>
        protected DbSet<TApplication> Applications => Context.Set<TApplication>();

        /// <summary>
        /// Gets the database set corresponding to the <typeparamref name="TAuthorization"/> entity.
        /// </summary>
        protected DbSet<TAuthorization> Authorizations => Context.Set<TAuthorization>();

        /// <summary>
        /// Gets the database set corresponding to the <typeparamref name="TToken"/> entity.
        /// </summary>
        protected DbSet<TToken> Tokens => Context.Set<TToken>();

        /// <summary>
        /// Determines the number of tokens that match the specified query.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the number of tokens that match the specified query.
        /// </returns>
        public override Task<long> CountAsync<TResult>([NotNull] Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return query(Tokens).LongCountAsync();
        }

        /// <summary>
        /// Creates a new token.
        /// </summary>
        /// <param name="token">The token to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public override Task CreateAsync([NotNull] TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            Context.Add(token);

            return Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Removes a token.
        /// </summary>
        /// <param name="token">The token to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>A <see cref="Task"/> that can be used to monitor the asynchronous operation.</returns>
        public override Task DeleteAsync([NotNull] TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            Context.Remove(token);

            return Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves the list of tokens corresponding to the specified application identifier.
        /// </summary>
        /// <param name="identifier">The application identifier associated with the tokens.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the tokens corresponding to the specified application.
        /// </returns>
        public override async Task<ImmutableArray<TToken>> FindByApplicationIdAsync([NotNull] string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("The identifier cannot be null or empty.", nameof(identifier));
            }

            // Note: due to a bug in Entity Framework Core's query visitor, the tokens can't be
            // filtered using token.Application.Id.Equals(key). To work around this issue,
            // this method is overriden to use an explicit join before applying the equality check.
            // See https://github.com/openiddict/openiddict-core/issues/499 for more information.

            IQueryable<TToken> Query(IQueryable<TApplication> applications, IQueryable<TToken> tokens, TKey key)
                => from token in tokens.Include(token => token.Application).Include(token => token.Authorization).AsTracking()
                   join application in applications.AsTracking() on token.Application.Id equals application.Id
                   where application.Id.Equals(key)
                   select token;

            return ImmutableArray.CreateRange(await Query(
                Applications, Tokens, ConvertIdentifierFromString(identifier)).ToListAsync(cancellationToken));
        }

        /// <summary>
        /// Retrieves the list of tokens corresponding to the specified authorization identifier.
        /// </summary>
        /// <param name="identifier">The authorization identifier associated with the tokens.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the tokens corresponding to the specified authorization.
        /// </returns>
        public override async Task<ImmutableArray<TToken>> FindByAuthorizationIdAsync([NotNull] string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("The identifier cannot be null or empty.", nameof(identifier));
            }

            // Note: due to a bug in Entity Framework Core's query visitor, the tokens can't be
            // filtered using token.Authorization.Id.Equals(key). To work around this issue,
            // this method is overriden to use an explicit join before applying the equality check.
            // See https://github.com/openiddict/openiddict-core/issues/499 for more information.

            IQueryable<TToken> Query(IQueryable<TAuthorization> authorizations, IQueryable<TToken> tokens, TKey key)
                => from token in tokens.Include(token => token.Application).Include(token => token.Authorization).AsTracking()
                   join authorization in authorizations.AsTracking() on token.Authorization.Id equals authorization.Id
                   where authorization.Id.Equals(key)
                   select token;

            return ImmutableArray.CreateRange(await Query(
                Authorizations, Tokens, ConvertIdentifierFromString(identifier)).ToListAsync(cancellationToken));
        }

        /// <summary>
        /// Executes the specified query and returns the first element.
        /// </summary>
        /// <typeparam name="TState">The state type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="state">The optional state.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the first element returned when executing the query.
        /// </returns>
        public override Task<TResult> GetAsync<TState, TResult>(
            [NotNull] Func<IQueryable<TToken>, TState, IQueryable<TResult>> query,
            [CanBeNull] TState state, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return query(
                Tokens.Include(token => token.Application)
                      .Include(token => token.Authorization)
                      .AsTracking(), state).FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Executes the specified query and returns all the corresponding elements.
        /// </summary>
        /// <typeparam name="TState">The state type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="state">The optional state.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns all the elements returned when executing the specified query.
        /// </returns>
        public override async Task<ImmutableArray<TResult>> ListAsync<TState, TResult>(
            [NotNull] Func<IQueryable<TToken>, TState, IQueryable<TResult>> query,
            [CanBeNull] TState state, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return ImmutableArray.CreateRange(await query(
                Tokens.Include(token => token.Application)
                      .Include(token => token.Authorization)
                      .AsTracking(), state).ToListAsync(cancellationToken));
        }

        /// <summary>
        /// Removes the tokens that are marked as expired or invalid.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public override async Task PruneAsync(CancellationToken cancellationToken = default)
        {
            // Note: Entity Framework Core doesn't support set-based deletes, which prevents removing
            // entities in a single command without having to retrieve and materialize them first.
            // To work around this limitation, entities are manually listed and deleted using a batch logic.

            IList<Exception> exceptions = null;

            IQueryable<TToken> Query(IQueryable<TToken> tokens, int offset)
                => (from token in tokens.AsTracking()
                    where token.ExpirationDate < DateTimeOffset.UtcNow ||
                          token.Status != OpenIddictConstants.Statuses.Valid
                    orderby token.Id
                    select token).Skip(offset).Take(1_000);

            async Task<IDbContextTransaction> CreateTransactionAsync()
            {
                // Note: transactions that specify an explicit isolation level are only supported by
                // relational providers and trying to use them with a different provider results in
                // an invalid operation exception being thrown at runtime. To prevent that, a manual
                // check is made to ensure the underlying transaction manager is relational.
                var manager = Context.Database.GetService<IDbContextTransactionManager>();
                if (manager is IRelationalTransactionManager)
                {
                    // Note: relational providers like Sqlite are known to lack proper support
                    // for repeatable read transactions. To ensure this method can be safely used
                    // with such providers, the database transaction is created in a try/catch block.
                    try
                    {
                        return await Context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
                    }

                    catch
                    {
                        return null;
                    }
                }

                return null;
            }

            for (var offset = 0; offset < 100_000; offset = offset + 1_000)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // To prevent concurrency exceptions from being thrown if an entry is modified
                // after it was retrieved from the database, the following logic is executed in
                // a repeatable read transaction, that will put a lock on the retrieved entries
                // and thus prevent them from being concurrently modified outside this block.
                using (var transaction = await CreateTransactionAsync())
                {
                    var tokens = await ListAsync((source, state) => Query(source, state), offset, cancellationToken);
                    if (tokens.IsEmpty)
                    {
                        break;
                    }

                    Context.RemoveRange(tokens);

                    try
                    {
                        await Context.SaveChangesAsync(cancellationToken);
                        transaction?.Commit();
                    }

                    catch (Exception exception)
                    {
                        if (exceptions == null)
                        {
                            exceptions = new List<Exception>(capacity: 1);
                        }

                        exceptions.Add(exception);
                    }
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException("An error occurred while pruning tokens.", exceptions);
            }
        }

        /// <summary>
        /// Sets the application identifier associated with a token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="identifier">The unique identifier associated with the client application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public override async Task SetApplicationIdAsync([NotNull] TToken token,
            [CanBeNull] string identifier, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!string.IsNullOrEmpty(identifier))
            {
                var key = ConvertIdentifierFromString(identifier);

                var application = await Applications.SingleOrDefaultAsync(element => element.Id.Equals(key), cancellationToken);
                if (application == null)
                {
                    throw new InvalidOperationException("The application associated with the token cannot be found.");
                }

                token.Application = application;
            }

            else
            {
                var key = await GetIdAsync(token, cancellationToken);

                // Try to retrieve the application associated with the token.
                // If none can be found, assume that no application is attached.
                var application = await Applications.FirstOrDefaultAsync(element => element.Tokens.Any(t => t.Id.Equals(key)));
                if (application != null)
                {
                    application.Tokens.Remove(token);
                }
            }
        }

        /// <summary>
        /// Sets the authorization identifier associated with a token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="identifier">The unique identifier associated with the authorization.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public override async Task SetAuthorizationIdAsync([NotNull] TToken token,
            [CanBeNull] string identifier, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!string.IsNullOrEmpty(identifier))
            {
                var key = ConvertIdentifierFromString(identifier);

                var authorization = await Authorizations.SingleOrDefaultAsync(element => element.Id.Equals(key), cancellationToken);
                if (authorization == null)
                {
                    throw new InvalidOperationException("The authorization associated with the token cannot be found.");
                }

                token.Authorization = authorization;
            }

            else
            {
                var key = await GetIdAsync(token, cancellationToken);

                // Try to retrieve the authorization associated with the token.
                // If none can be found, assume that no authorization is attached.
                var authorization = await Authorizations.FirstOrDefaultAsync(element => element.Tokens.Any(t => t.Id.Equals(key)));
                if (authorization != null)
                {
                    authorization.Tokens.Remove(token);
                }
            }
        }

        /// <summary>
        /// Updates an existing token.
        /// </summary>
        /// <param name="token">The token to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public override Task UpdateAsync([NotNull] TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            Context.Attach(token);

            // Generate a new concurrency token and attach it
            // to the token before persisting the changes.
            token.ConcurrencyToken = Guid.NewGuid().ToString();

            Context.Update(token);

            return Context.SaveChangesAsync(cancellationToken);
        }
    }
}