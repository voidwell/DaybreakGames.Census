namespace DemoApp.Models;

public class CensusCharacterModel
{
    public string CharacterId { get; set; } = null!;
    public CharacterName Name { get; set; } = null!;
    public int FactionId { get; set; }
    public int TitleId { get; set; }
    public CharacterTimes Times { get; set; } = null!;
    public CharacterBattleRank BattleRank { get; set; } = null!;
    public CharacterCerts Certs { get; set; } = null!;
    public int WorldId { get; set; }
    public bool OnlineStatus { get; set; }
    public int PrestigeLevel { get; set; }

    public class CharacterName
    {
        public string First { get; set; } = null!;
        public string FirstLower { get; set; } = null!;
    }

    public class CharacterTimes
    {
        public DateTime CreationDate { get; set; }
        public DateTime LastSaveDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public int MinutesPlayed { get; set; }
    }

    public class CharacterBattleRank
    {
        public int PercentToNext { get; set; }
        public int Value { get; set; }
    }

    public class CharacterCerts
    {
        public int EarnedPoints { get; set; }
        public int GiftedPoints { get; set; }
        public int SpentPoints { get; set; }
        public int AvailablePoints { get; set; }
        public float PercentToNext { get; set; }
    }
}
