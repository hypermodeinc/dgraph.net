/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using Dgraph.Api;
using Dgraph.Transactions;
using FluentResults;
using Grpc.Core;
using Grpc.Net.Client;

// For unit testing.  Allows to make mocks of the internal interfaces and factories
// so can test in isolation from a Dgraph instance.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Dgraph.tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")] // for NSubstitute

namespace Dgraph
{
    public class DgraphClient : IDgraphClient, IDgraphClientInternal
    {
        public static IDgraphClient Create(params GrpcChannel[] channels)
        {
            return new DgraphClient(channels);
        }

        private readonly List<Api.Dgraph.DgraphClient> _clients;
        private readonly GrpcChannel[] _channels;

        private DgraphClient(params GrpcChannel[] channels)
        {
            _channels = channels;
            _clients = [.. channels.Select(c => new Api.Dgraph.DgraphClient(c))];
        }

        #region IDgraphClient

#if NETFRAMEWORK
        Task<Result> IDgraphClient.Login(string user, string password, CallOptions? options)
        {
            return ((IDgraphClient)this).LoginIntoNamespace(user, password, 0, options);
        }
#endif 

        Task<Result> IDgraphClient.LoginIntoNamespace(string user, string password, ulong ns, CallOptions? options)
        {
            return DgraphExecute(
                async (dg) =>
                {
                    await dg.LoginAsync(new LoginRequest
                    {
                        Userid = user,
                        Password = password,
                        Namespace = ns
                    }, options ?? new CallOptions());
                    return Result.Ok();
                },
                (rpcEx) => Result.Fail(new ExceptionalError(rpcEx))
            );
        }

        Task<Result> IDgraphClient.Alter(Operation op, CallOptions? options)
        {
            return DgraphExecute(
                async (dg) =>
                {
                    await dg.AlterAsync(op, options ?? new CallOptions());
                    return Result.Ok();
                },
                (rpcEx) => Result.Fail(new ExceptionalError(rpcEx))
            );
        }

        Task<Result<string>> IDgraphClient.CheckVersion(CallOptions? options)
        {
            return DgraphExecute(
                async (dg) =>
                {
                    var versionResult = await dg.CheckVersionAsync(new Check(), options ?? new CallOptions());
                    return Result.Ok(versionResult.Tag); ;
                },
                (rpcEx) => Result.Fail<string>(new ExceptionalError(rpcEx))
            );
        }

        ITransaction IDgraphClient.NewTransaction()
        {
            AssertNotDisposed();
            return new Transaction(client: this);
        }

        IQuery IDgraphClient.NewReadOnlyTransaction(bool bestEffort)
        {
            AssertNotDisposed();
            return new Transaction(client: this, readOnly: true, bestEffort: bestEffort);
        }

        #endregion

        #region execution

        public async Task<T> DgraphExecute<T>(
            Func<Api.Dgraph.DgraphClient, Task<T>> execute,
            Func<RpcException, T> onFail
        )
        {
            AssertNotDisposed();

            try
            {
                // Randomly pick the next client to use.
#if NETFRAMEWORK || NETSTANDARD
                var nextClient = _clients[RandomPolyfill.Shared.Next(_clients.Count)];
#else
                var nextClient = _clients[Random.Shared.Next(_clients.Count)];
#endif

                return await execute(nextClient);
            }
            catch (RpcException rpcEx)
            {
                return onFail(rpcEx);
            }
        }

        #endregion

        #region IDisposable

        private bool Disposed = false;

        protected void AssertNotDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose()
        {
            DisposeIDisposables();
        }

        protected virtual void DisposeIDisposables()
        {
            if (!Disposed)
            {
                Disposed = true;
                foreach (var channel in _channels)
                {
                    channel.Dispose();
                }
            }
        }

        #endregion
    }
}
