using Mongo.Migration.Migrations.Database;
using MongoDB.Driver;
using NoSqlModels.MigrationObjects;

namespace MongoDbTest.Migrations
{
    public class AddNullableBool() : DatabaseMigration("1.0.1")
    {
        private readonly string collectionName = MongoDbContext.GetCollectionName<MigrationObject>();

        public override void Up(IMongoDatabase db)
        {
            var filter = Builders<MigrationObject_V1>.Filter.Empty;
            var update = Builders<MigrationObject_V1>.Update.Set(c => c.IsDefault, true);
            var col = db.GetCollection<MigrationObject_V1>(collectionName);
            col.UpdateMany(filter, update);
        }

        public override void Down(IMongoDatabase db)
        {
            var filter = Builders<MigrationObject_V1>.Filter.Empty;
            var update = Builders<MigrationObject_V1>.Update.Unset(c => c.IsDefault);
            var col = db.GetCollection<MigrationObject_V1>(collectionName);
            col.UpdateMany(filter, update);
        }
    }
}
