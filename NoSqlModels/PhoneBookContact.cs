﻿namespace NoSqlModels;

public class PhoneBookContact : IIdItem
{
    public string Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string UserId { get; set; }

    public string Location { get; set; }

    public DateTime? LastUpdate { get; set; }

    public string LastUpdateBy { get; set; }

    public List<PhoneBookContactNumber> Numbers { get; set; }

    [DependencyField(nameof(SecretaryIds))]

    public List<PhoneBookContact> Secretary { get; set; }

    public List<string> SecretaryIds { get; set; }

    public string? ManagerId { get; set; }

    [DependencyField(nameof(ManagerId))]
    public PhoneBookContact Manager { get; set; }

    [DependencyField(nameof(CategoryIds))]
    public List<PhoneBookCategory> Categories { get; set; }

    public List<string> CategoryIds { get; set; } = [];

    [DependencyField(nameof(PhoneBookIds))]
    public List<PhoneBook> PhoneBooks { get; set; }

    public List<string> PhoneBookIds { get; set; } = [];

    public int NumberOfTelephoneNumbers => Numbers?.Count ?? 0;
}

public class PhoneBookContactNumber : IIdItem
{
    public string Id { get; set; }

    public string Number { get; set; }

    public NumberType Type { get; set; }
}

public enum NumberType { Office, Mobile, Home }

public class PhoneBookContactSearchParameters : GenericSearchParameters
{
    public string Location { get; set; }

    public string Number { get; set; }

    public List<string> CategoryIds { get; set; }

    public List<string> ManagerIds { get; set; }

    public List<string> SecretaryIds { get; set; }
}