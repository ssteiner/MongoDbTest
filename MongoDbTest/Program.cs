using MongoDbTest;

MongoDbTester mongoDbTester = new();
await mongoDbTester.RunTest().ConfigureAwait(false);