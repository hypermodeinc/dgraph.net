/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

namespace Dgraph.Schema
{
    public class DgraphSchema
    {
        public required List<DgraphPredicate> Schema { get; set; }

        public required List<DgraphType> Types { get; set; }

        public override string ToString()
        {
            var preds = string.Join("\n", Schema.Select(p => p.ToString()));
            var types = string.Join("\n\n", Types?.Select(t => t.ToString()) ?? []);
            return preds + (types.Length > 0 ? "\n" + types + "\n" : "\n");
        }
    }
}
