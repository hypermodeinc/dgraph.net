/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

namespace Dgraph.Schema
{
    public class DgraphField
    {
        public required string Name { get; set; }

        public override string ToString() => Name;
    }
}
