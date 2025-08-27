/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using FluentResults;
using Grpc.Core;

namespace Dgraph.Transactions
{

    /// <summary>
    /// A read-only transaction that cannot commit mutations.
    /// </summary>
    public interface IQuery
    {
        TransactionState TransactionState { get; }

        /// <summary>
        /// Run a query and return a JSON response.
        /// </summary>
#if NETFRAMEWORK
        Task<Result<Response>> Query(string queryString, CallOptions? options = null);
#else
        Task<Result<Response>> Query(string queryString, CallOptions? options = null)
        {
            return QueryWithVars(queryString, [], options);
        }
#endif

        /// <summary>
        /// Run a query with variables and return a JSON response.
        /// </summary>
        Task<Result<Response>> QueryWithVars(
            string queryString,
            Dictionary<string, string>? varMap,
            CallOptions? options = null
        );

        /// <summary>
        /// Run a query with variables and return a RDF response.
        /// </summary>
#if NETFRAMEWORK
        Task<Result<Response>> QueryRDF(string queryString, CallOptions? options = null);
#else
        Task<Result<Response>> QueryRDF(string queryString, CallOptions? options = null)
        {
            return QueryRDFWithVars(queryString, [], options);
        }
#endif

        /// <summary>
        /// Run a query and return a RDF response.
        /// </summary>
        Task<Result<Response>> QueryRDFWithVars(
            string queryString,
            Dictionary<string, string> varMap,
            CallOptions? options = null
        );
    }
}
