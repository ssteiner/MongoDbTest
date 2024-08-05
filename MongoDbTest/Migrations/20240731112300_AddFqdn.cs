using Mongo.Migration.Migrations.Database;
using MongoDB.Driver;
using NoSqlModels.MigrationObjects;

namespace MongoDbTest.Migrations
{
    public class AddFqdn() : DatabaseMigration("1.0.2")
    {
        private readonly string collectionName = MongoDbContext.GetCollectionName<MigrationObject>();
        public override void Up(IMongoDatabase db)
        {
            var filter = Builders<MigrationObject_V2>.Filter.Empty;
            var col = db.GetCollection<MigrationObject_V2>(collectionName);
            //foreach (var item in col.FindSync(filter).ToList())
            //{
            //    var update = Builders<MigrationObject_V2>.Update.Set(c => c.Fqdn, item.Name);
            //    col.UpdateOne(Builders<MigrationObject_V2>.Filter.Eq(c => c.Id, item.Id), update);
            //}
            //var updatePipeline = "{{ '$set': {{ 'Fqdn': {{ '$Name' }} }} }}";

            var pipeline = new EmptyPipelineDefinition<MigrationObject_V2>()
                .AppendStage<MigrationObject_V2, MigrationObject_V2, MigrationObject_V2>("{ $set: { Fqdn: '$Name' } }");
                //.AppendStage<Article, Article, Article>($"{{ $set: {{ 'secondary.updatedAt': {{$date: '{DateTime.UtcNow}' }} }} }}");
            var update = Builders<MigrationObject_V2>.Update.Pipeline(pipeline);
            var res = col.UpdateMany(filter, update);
        }

        public override void Down(IMongoDatabase db)
        {
            var filter = Builders<MigrationObject_V2>.Filter.Empty;
            var update = Builders<MigrationObject_V2>.Update.Unset(c => c.Fqdn);
            var col = db.GetCollection<MigrationObject_V2>(collectionName);
            var res = col.UpdateMany(filter, update);
        }
    }
}
