namespace MongoDbTest;

internal class Program
{
    static async Task Main(string[] args)
    {
        MongoDbTester mongoDbTester = new();
        await mongoDbTester.RunTest();
    }
}
