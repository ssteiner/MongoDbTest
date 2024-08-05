using Mongo.Migration.Migrations.Database;
using MongoDB.Bson;
using MongoDB.Driver;
using NoSqlModels.MigrationObjects;

namespace MongoDbTest.Migrations
{
    public class AddAndPopulateNewTable() : DatabaseMigration("1.0.3")
    {
        private readonly string collectionName = MongoDbContext.GetCollectionName<MigrationObject>();
        private readonly string otherCollectionName = MongoDbContext.GetCollectionName<OtherMigrationObject>();

        public override void Up(IMongoDatabase db)
        {
            var obj1 = new OtherMigrationObject { Id = ObjectId.GenerateNewId().ToString(), IntIdentifier = 1, Name = "Number 1" };
            var obj2 = new OtherMigrationObject { Id = ObjectId.GenerateNewId().ToString(), IntIdentifier = 2, Name = "Number 2" };
            var obj3 = new OtherMigrationObject { Id = ObjectId.GenerateNewId().ToString(), IntIdentifier = 3, Name = "Number 3" };
            List<OtherMigrationObject> migrationObjects = [obj1, obj2, obj3];
            db.CreateCollection(otherCollectionName);

            var col = db.GetCollection<OtherMigrationObject>(otherCollectionName);

            col.InsertMany(migrationObjects, new InsertManyOptions { IsOrdered = true });

            var migrationObjectsCollection = db.GetCollection<MigrationObject_V3>(collectionName);
            var filter = Builders<MigrationObject_V3>.Filter.Empty;

            var allMigrationObjects = migrationObjectsCollection.Find(filter).ToList();
            var rand = new Random();
            foreach (var migrationObject in allMigrationObjects)
            {
                var nextIndex = rand.Next(migrationObjects.Count);
                var obj = migrationObjects[nextIndex];

                var update = Builders<MigrationObject_V3>.Update.Set(c => c.OtherObjectId, obj.Id);
                var res = migrationObjectsCollection.UpdateOne(u => u.Id == migrationObject.Id, update);
            }
        }

        public override void Down(IMongoDatabase db)
        {
            //var col = db.GetCollection<OtherMigrationObject>(otherCollectionName);
            //var filter = Builders<OtherMigrationObject>.Filter.Empty;
            //col.DeleteMany(filter);
            var filter = Builders<MigrationObject_V3>.Filter.Empty;
            var update = Builders<MigrationObject_V3>.Update.Unset(c => c.OtherObjectId);
            var col = db.GetCollection<MigrationObject_V3>(collectionName);
            var res = col.UpdateMany(filter, update);
            db.DropCollection(otherCollectionName);
        }
    }
}
