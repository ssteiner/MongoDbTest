using Mongo.Migration.Migrations.Database;
using MongoDB.Bson;
using MongoDB.Driver;
using NoSqlModels.MigrationObjects;

namespace MongoDbTest.Migrations
{
    public class InitialPopulate() : DatabaseMigration("1.0.0")
    {
        private readonly string collectionName = MongoDbContext.GetCollectionName<MigrationObject>();

        public override void Up(IMongoDatabase db)
        {
            db.CreateCollection(collectionName);
            var col = db.GetCollection<MigrationObject>(collectionName);
            List<MigrationObject> migrationObjects = [
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "one" },
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "two" },
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "three" },
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "four" },
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "five" },
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "six" },
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "seven" },
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "eight" },
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "nine" },
                new MigrationObject { Id = ObjectId.GenerateNewId().ToString(), Name = "ten" },
                ];
            col.InsertMany(migrationObjects, new InsertManyOptions { IsOrdered = true });
        }

        public override void Down(IMongoDatabase db)
        {
            //var col = db.GetCollection<MigrationObject>(collectionName);
            //var filter = Builders<MigrationObject>.Filter.Empty;
            //col.DeleteMany(filter);
            db.DropCollection(collectionName);
        }
    }
}
