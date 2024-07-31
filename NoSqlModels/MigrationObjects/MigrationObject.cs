namespace NoSqlModels.MigrationObjects
{
    public class MigrationObject: IIdItem
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }
}
