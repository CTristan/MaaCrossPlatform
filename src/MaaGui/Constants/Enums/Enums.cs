namespace MaaGui.Constants.Enums;

public enum FightStageResetMode
{
    None = 0,
    Current = 1,
    All = 2,
}

public enum InfrastMode
{
    Normal = 0,
    Custom = 1,
}

public enum InfrastRoomType
{
    Control = 0,
    Power = 1,
    Manufacture = 2,
    Trading = 3,
    Dormitory = 4,
    Reception = 5,
    Workshop = 6,
    Training = 7,
    Office = 8,
}

public enum RoguelikeTheme
{
    Phantom = 0,
    Mizuki = 1,
    Sami = 2,
    Sarkaz = 3,
    JieGarden = 4,
}

public enum RoguelikeMode
{
    Exp = 0,
    Investment = 1,
    Collectible = 4,
    CLP_PDS = 5,
    Squad = 6,
    Exploration = 7,
    FindPlaytime = 20001,
}

[System.Flags]
public enum RoguelikeCollectibleAward
{
    None = 0,
    HotWater = 1,
    Hope = 2,
    Idea = 4,
}

public enum RoguelikeBoskySubNodeType
{
    Unknown = 0,
    Ling = 1,
    Shu = 2,
    Nian = 3,
}

public enum ReclamationTheme
{
    Fire = 0,
    Tales = 1,
}

public enum ReclamationMode
{
    NoArchive = 0,
    Archive = 1,
}
