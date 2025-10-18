namespace TwitchDropBot.Service.Models;

public class Drop
{
    public string endAt { get; set; }
    public string gameBoxArtURL { get; set; }
    public string gameDisplayName { get; set; }
    public string gameId { get; set; }
    public Rewards[] rewards { get; set; }
    public string startAt { get; set; }
}

public class Rewards
{
    public string id { get; set; }
    public Self self { get; set; }
    public Allow allow { get; set; }
    public string accountLinkURL { get; set; }
    public string description { get; set; }
    public string detailsURL { get; set; }
    public string endAt { get; set; }
    public object[] eventBasedDrops { get; set; }
    public Game game { get; set; }
    public string imageURL { get; set; }
    public string name { get; set; }
    public Owner owner { get; set; }
    public string startAt { get; set; }
    public string status { get; set; }
    public TimeBasedDrops[] timeBasedDrops { get; set; }
    public string __typename { get; set; }
}

public class Self
{
    public bool isAccountConnected { get; set; }
    public string __typename { get; set; }
}

public class Allow
{
    public Channels[] channels { get; set; }
    public bool isEnabled { get; set; }
    public string __typename { get; set; }
}

public class Channels
{
    public string id { get; set; }
    public string displayName { get; set; }
    public string name { get; set; }
    public string __typename { get; set; }
}

public class Game
{
    public string id { get; set; }
    public string slug { get; set; }
    public string displayName { get; set; }
    public string __typename { get; set; }
}

public class Owner
{
    public string id { get; set; }
    public string name { get; set; }
    public string __typename { get; set; }
}

public class TimeBasedDrops
{
    public string id { get; set; }
    public int requiredSubs { get; set; }
    public BenefitEdges[] benefitEdges { get; set; }
    public string endAt { get; set; }
    public string name { get; set; }
    public object preconditionDrops { get; set; }
    public int requiredMinutesWatched { get; set; }
    public string startAt { get; set; }
    public string __typename { get; set; }
}

public class BenefitEdges
{
    public Benefit benefit { get; set; }
    public int entitlementLimit { get; set; }
    public string __typename { get; set; }
}

public class Benefit
{
    public string id { get; set; }
    public string createdAt { get; set; }
    public int entitlementLimit { get; set; }
    public Game1 game { get; set; }
    public string imageAssetURL { get; set; }
    public bool isIosAvailable { get; set; }
    public string name { get; set; }
    public OwnerOrganization ownerOrganization { get; set; }
    public string distributionType { get; set; }
    public string __typename { get; set; }
}

public class Game1
{
    public string id { get; set; }
    public string name { get; set; }
    public string __typename { get; set; }
}

public class OwnerOrganization
{
    public string id { get; set; }
    public string name { get; set; }
    public string __typename { get; set; }
}

