/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

namespace Dgraph.Transactions
{
    public enum TransactionState
    {
        OK,
        Committed,
        Aborted,
        Error
    }
}
