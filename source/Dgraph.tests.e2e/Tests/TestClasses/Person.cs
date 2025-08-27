/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dgraph.tests.e2e.Tests.TestClasses
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class Person
    {
        public required string Uid { get; set; }

        [JsonProperty("dgraph.type")]
        public string Type { get; } = "Person";

        public required string Name { get; set; }

        public List<Person> Friends { get; } = [];

        public DateTime Dob { get; set; }

        public double Height { get; set; }

        public List<int> Scores { get; } = [];
    }
}
