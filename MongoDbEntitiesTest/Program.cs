// See https://aka.ms/new-console-template for more information
using MongoDbEntitiesTest;

MongoDbTester mongoDbTester = new();
await mongoDbTester.RunTest();
