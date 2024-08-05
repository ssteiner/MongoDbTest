using Mongo.Migration.Migrations.Database;
using MongoDB.Driver;
using NoSqlModels.MigrationObjects;

namespace MongoDbTest.Migrations
{
    public class RenameProperty() : DatabaseMigration("1.0.5")
    {
        private readonly string collectionName = MongoDbContext.GetCollectionName<MigrationObject>();

        public override void Up(IMongoDatabase db)
        {
            var col = db.GetCollection<MigrationObject_V3>(collectionName);
            var filter = Builders<MigrationObject_V3>.Filter.Exists(x => x.IntIdentifier);
            var update = Builders<MigrationObject_V3>.Update.Rename(c => c.IntIdentifier, nameof(MigrationObject_V4.IntIdentifier2));
            var res = col.UpdateMany(filter, update);
        }

        public override void Down(IMongoDatabase db)
        {
            var col = db.GetCollection<MigrationObject_V4>(collectionName);
            var filter = Builders<MigrationObject_V4>.Filter.Exists(x => x.IntIdentifier2);
            var update = Builders<MigrationObject_V4>.Update.Rename(c => c.IntIdentifier2, nameof(MigrationObject_V3.IntIdentifier));
            var res = col.UpdateMany(filter, update);
        }
    }
}
