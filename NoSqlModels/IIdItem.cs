﻿namespace NoSqlModels;

public interface IIdItem
{
    string Id { get; set; }
}

public interface INamedItem
{
    string Name { get; set; }
}

public interface IDescriptionItem
{
    string Description { get; set; }
}