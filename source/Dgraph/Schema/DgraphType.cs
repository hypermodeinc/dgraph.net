/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

namespace Dgraph.Schema
{
    public class DgraphType
    {
        public required string Name { get; set; }

        public required List<DgraphField> Fields { get; set; }

        public override string ToString()
        {
            string fields = string.Join("\n", Fields.Select(f => "\t" + f));
            return $"type {Name} {{\n{fields}\n}}";
        }
    }
}
