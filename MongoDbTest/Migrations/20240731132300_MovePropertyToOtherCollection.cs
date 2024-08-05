using Mongo.Migration.Migrations.Database;
using MongoDB.Driver;
using NoSqlModels.MigrationObjects;

namespace MongoDbTest.Migrations
{
    public class MovePropertyToOtherCollection() : DatabaseMigration("1.0.4")
    {
        private readonly string collectionName = MongoDbContext.GetCollectionName<MigrationObject>();
        private readonly string otherCollectionName = MongoDbContext.GetCollectionName<OtherMigrationObject>();

        public override void Up(IMongoDatabase db)
        {
            var col = db.GetCollection<MigrationObject_V3>(collectionName);

            var migrationObjectsWithoutIntIdentifier = db.GetCollection<MigrationObject_V3>(collectionName)
                .AsQueryable()
                .Where(u => u.IntIdentifier == null)
                .Select(x => new MigrationObject_V3 { Id = x.Id, OtherObjectId = x.OtherObjectId })
                .ToList();

            var otherObjectsWithIdentifier = db.GetCollection<OtherMigrationObject>(otherCollectionName)
                .AsQueryable()
                .Where(u => u.IntIdentifier != null)
                .ToList();

            foreach (var otherObject in otherObjectsWithIdentifier)
            {
                var filter = Builders<MigrationObject_V3>.Filter.Eq(x => x.OtherObjectId, otherObject.Id);
                var update = Builders<MigrationObject_V3>.Update.Set(c => c.IntIdentifier, otherObject.IntIdentifier);
                var res = col.UpdateMany(filter, update);
            }
        }

        public override void Down(IMongoDatabase db)
        {
            var filter = Builders<MigrationObject_V3>.Filter.Empty;
            var update = Builders<MigrationObject_V3>.Update.Unset(c => c.IntIdentifier);
            var col = db.GetCollection<MigrationObject_V3>(collectionName);
            var res = col.UpdateMany(filter, update);
        }
    }
}
