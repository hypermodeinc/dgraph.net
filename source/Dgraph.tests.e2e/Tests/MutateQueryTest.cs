/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using Dgraph.tests.e2e.Orchestration;
using Dgraph.tests.e2e.Tests.TestClasses;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dgraph.tests.e2e.Tests
{
    public class MutateQueryTest : DgraphDotNetE2ETest
    {

        private readonly Person _person1, _person2, _person3;

        public MutateQueryTest(DgraphClientFactory clientFactory) : base(clientFactory)
        {
            _person1 = new Person()
            {
                Uid = "_:Person1",
                Name = "Person1",
                Dob = new DateTime(1991, 1, 1),
                Height = 1.78,
                Scores = { 1, 32, 45, 62 }
            };

            _person2 = new Person()
            {
                Uid = "_:Person2",
                Name = "Person2",
                Dob = new DateTime(1992, 1, 1),
                Height = 1.85,
                Scores = { 3, 2, 1 }
            };

            _person3 = new Person()
            {
                Uid = "_:Person3",
                Name = "Person3",
                Dob = new DateTime(1993, 1, 1),
                Height = 1.85,
                Scores = { 32, 21, 10 },
            };
            _person3.Friends.AddRange([_person1]);
        }

        public async override Task Setup()
        {
            await base.Setup();

            var client = await ClientFactory.GetDgraphClient();
            var result = await client.Alter(
                new Api.Operation
                {
                    Schema = ReadEmbeddedFile("test.schema")
                });
            AssertResultIsSuccess(result);
        }

        public async override Task Test()
        {
            using var client = await ClientFactory.GetDgraphClient();

            await AddThreePeople(client);
            await QueryAllThreePeople(client);
            await AlterAPerson(client);
            await QueryWithVars(client);
            await DeleteAPerson(client);
        }

        private async Task AddThreePeople(IDgraphClient client)
        {
            using var transaction = client.NewTransaction();

            // Serialize the objects to json in whatever way works best for you.
            //
            // The CamelCaseNamingStrategy attribute on the type means these
            // get serialized with initial lower case.
            var personList = new List<Person> { _person1, _person2, _person3 };
            var json = JsonConvert.SerializeObject(personList);

            // There's two mutation options.  For just one mutation, use this version
            var result = await transaction.Mutate(new MutationBuilder().SetJson(json));

            // For more complicated multi-mutation requests or upsert mutations
            // see the upsert test.

            AssertResultIsSuccess(result, "Mutation failed");

            // The payload of the result contains a node->uid map of newly
            // allocated nodes.  If the nodes don't have uid names in the
            // mutation, then the map is like
            //
            // {{ "blank-0": "0xa", "blank-1": "0xb", "blank-2": "0xc", ... }}
            //
            // If the
            // mutation has '{ "uid": "_:Person1" ... }' etc, then the blank
            // node map is like
            // 
            // {{ "Person3": "0xe", "Person1": "0xf", "Person2": "0xd", ... }}

            result.Value.Uids.Count.Should().Be(3);

            // It's no required to save the uid's like this, but can work
            // nicely ... and makes these tests easier to keep track of.

            _person1.Uid = result.Value.Uids[_person1.Uid.Substring(2)];
            _person2.Uid = result.Value.Uids[_person2.Uid.Substring(2)];
            _person3.Uid = result.Value.Uids[_person3.Uid.Substring(2)];

            var transactionResult = await transaction.Commit();
            AssertResultIsSuccess(transactionResult);

        }

        private async Task QueryAllThreePeople(IDgraphClient client)
        {
            var people = new List<Person> { _person1, _person2, _person3 };

            foreach (var person in people)
            {
                var queryPerson = await client.NewReadOnlyTransaction().
                    Query(FriendQueries.QueryByUid(person.Uid));
                AssertResultIsSuccess(queryPerson, "Query failed");

                // the query result is json like { q: [ ...Person... ] }
                FriendQueries.AssertStringIsPerson(queryPerson.Value.Json, person);
            }
        }

        private async Task AlterAPerson(IDgraphClient client)
        {
            using var transaction = client.NewTransaction();

            _person3.Friends.Add(_person2);

            // This will serialize the whole object.  You might not want to
            // do that, and maybe only add in the bits that have changed
            // instead.
            var json = JsonConvert.SerializeObject(_person3);

            var result = await transaction.Mutate(new MutationBuilder().SetJson(json));
            AssertResultIsSuccess(result, "Mutation failed");

            // no nodes were allocated
            result.Value.Uids.Count.Should().Be(0);

            var transactionResult = await transaction.Commit();
            AssertResultIsSuccess(transactionResult);

            var queryPerson = await client.NewReadOnlyTransaction().
                    Query(FriendQueries.QueryByUid(_person3.Uid));
            AssertResultIsSuccess(queryPerson, "Query failed");

            FriendQueries.AssertStringIsPerson(queryPerson.Value.Json, _person3);
        }

        private async Task QueryWithVars(IDgraphClient client)
        {

            var queryPerson = await client.NewReadOnlyTransaction().QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", _person3.Name } });
            AssertResultIsSuccess(queryPerson, "Query failed");

            FriendQueries.AssertStringIsPerson(queryPerson.Value.Json, _person3);
        }

        private async Task DeleteAPerson(IDgraphClient client)
        {
            using var transaction = client.NewTransaction();

            // delete a node by passing JSON like this to delete
            var deleteResult = await transaction.Mutate(
                new MutationBuilder().DeleteJson($"{{\"uid\": \"{_person1.Uid}\"}}")
            );
            AssertResultIsSuccess(deleteResult, "Delete failed");

            var transactionResult = await transaction.Commit();
            AssertResultIsSuccess(transactionResult);

            // that person should be gone...
            var queryPerson1 = await client.NewReadOnlyTransaction().
                Query(FriendQueries.QueryByUid(_person1.Uid));
            AssertResultIsSuccess(queryPerson1, "Query failed");

            // no matter what uid you query for, Dgraph always succeeds :-(
            // e.g. on a fresh dgraph with no uids allocated
            // { q(func: uid(0x44444444)) { uid }} 
            // will answer
            // "q": [ { "uid": "0x44444444" } ] 
            //
            // so the only way to test that the node is deleted, 
            // is to test that we got only that back

            queryPerson1.Value.Json.Should().Be($"{{\"q\":[{{\"uid\":\"{_person1.Uid}\"}}]}}");

            // ... but watch out, Dgraph can leave dangling references 
            // e.g. there are some edges in our graph that still point to
            // Person 1 - we've just removed all it's outgoing edges.
            var queryPerson3 = await client.NewReadOnlyTransaction().
                Query(FriendQueries.QueryByUid(_person3.Uid));
            AssertResultIsSuccess(queryPerson3, "Query failed");
            var person3 = JObject.Parse(queryPerson3.Value.Json)?["q"]?[0]?.ToObject<Person>();
            person3.Should().NotBeNull();

            person3.Friends.Count.Should().Be(2);
        }
    }
}
