using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using MaaGui.Constants.Enums;
using TaskType = MaaGui.Constants.TaskType;

namespace MaaGui.Configuration.Tasks;

[JsonDerivedType(typeof(StartUpTask), typeDiscriminator: "StartUpTask")]
[JsonDerivedType(typeof(FightTask), typeDiscriminator: "FightTask")]
[JsonDerivedType(typeof(InfrastTask), typeDiscriminator: "InfrastTask")]
[JsonDerivedType(typeof(MallTask), typeDiscriminator: "MallTask")]
[JsonDerivedType(typeof(RecruitTask), typeDiscriminator: "RecruitTask")]
[JsonDerivedType(typeof(AwardTask), typeDiscriminator: "AwardTask")]
[JsonDerivedType(typeof(RoguelikeTask), typeDiscriminator: "RoguelikeTask")]
[JsonDerivedType(typeof(ReclamationTask), typeDiscriminator: "ReclamationTask")]
[JsonDerivedType(typeof(CopilotTask), typeDiscriminator: "CopilotTask")]
[JsonDerivedType(typeof(CustomTask), typeDiscriminator: "CustomTask")]
public class BaseTask
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonIgnore]
    public TaskType TaskType { get; protected set; } = TaskType.Unknown;
}

public class StartUpTask : BaseTask
{
    public StartUpTask() => TaskType = TaskType.StartUp;

    [JsonPropertyName("account_name")]
    public string AccountName { get; set; } = string.Empty;
}

public class FightTask : BaseTask
{
    public FightTask() => TaskType = TaskType.Fight;

    [JsonPropertyName("use_medicine")]
    public bool UseMedicine { get; set; } = false;

    [JsonPropertyName("medicine_count")]
    public int MedicineCount { get; set; }

    [JsonPropertyName("use_stone")]
    public bool UseStone { get; set; } = false;

    [JsonPropertyName("stone_count")]
    public int StoneCount { get; set; }

    [JsonPropertyName("enable_target_drop")]
    public bool EnableTargetDrop { get; set; } = false;

    [JsonPropertyName("drop_id")]
    public string DropId { get; set; } = string.Empty;

    [JsonPropertyName("drop_count")]
    public int DropCount { get; set; }

    [JsonPropertyName("enable_times_limit")]
    public bool EnableTimesLimit { get; set; } = false;

    [JsonPropertyName("times_limit")]
    public int TimesLimit { get; set; } = int.MaxValue;

    [JsonPropertyName("stage_plan")]
    public List<string> StagePlan { get; set; } = [string.Empty];

    [JsonPropertyName("use_custom_annihilation")]
    public bool UseCustomAnnihilation { get; set; }

    [JsonPropertyName("annihilation_stage")]
    public string AnnihilationStage { get; set; } = "Annihilation";

    [JsonPropertyName("use_weekly_schedule")]
    public bool UseWeeklySchedule { get; set; }

    [JsonPropertyName("weekly_schedule")]
    public Dictionary<DayOfWeek, bool> WeeklySchedule { get; set; } = Enum.GetValues<DayOfWeek>().ToDictionary(i => i, _ => true);
}

public class InfrastTask : BaseTask
{
    public InfrastTask() => TaskType = TaskType.Infrast;

    [JsonPropertyName("mode")]
    public InfrastMode Mode { get; set; } = InfrastMode.Normal;

    [JsonPropertyName("uses_of_drones")]
    public string UsesOfDrones { get; set; } = "Money";

    [JsonPropertyName("dorm_threshold")]
    public int DormThreshold { get; set; } = 30;

    [JsonPropertyName("dorm_trust_enabled")]
    public bool DormTrustEnabled { get; set; } = true;

    [JsonPropertyName("originium_shard_auto_replenishment")]
    public bool OriginiumShardAutoReplenishment { get; set; } = true;

    [JsonPropertyName("reception_message_board")]
    public bool ReceptionMessageBoard { get; set; } = true;

    [JsonPropertyName("reception_clue_exchange")]
    public bool ReceptionClueExchange { get; set; } = true;

    [JsonPropertyName("send_clue")]
    public bool SendClue { get; set; } = true;
}

public class MallTask : BaseTask
{
    public MallTask() => TaskType = TaskType.Mall;

    [JsonPropertyName("shopping")]
    public bool Shopping { get; set; } = true;

    [JsonPropertyName("visit_friends")]
    public bool VisitFriends { get; set; } = true;

    [JsonPropertyName("only_buy_discount")]
    public bool OnlyBuyDiscount { get; set; }

    [JsonPropertyName("reserve_max_credit")]
    public bool ReserveMaxCredit { get; set; }
}

public class RecruitTask : BaseTask
{
    public RecruitTask() => TaskType = TaskType.Recruit;

    [JsonPropertyName("max_times")]
    public int MaxTimes { get; set; } = 4;

    [JsonPropertyName("refresh_level3")]
    public bool RefreshLevel3 { get; set; } = true;

    [JsonPropertyName("level3_choose")]
    public bool Level3Choose { get; set; } = true;

    [JsonPropertyName("level4_choose")]
    public bool Level4Choose { get; set; } = true;

    [JsonPropertyName("level5_choose")]
    public bool Level5Choose { get; set; }
}

public class AwardTask : BaseTask
{
    public AwardTask() => TaskType = TaskType.Award;

    [JsonPropertyName("award")]
    public bool Award { get; set; } = true;

    [JsonPropertyName("mail")]
    public bool Mail { get; set; }

    [JsonPropertyName("free_gacha")]
    public bool FreeGacha { get; set; }

    [JsonPropertyName("orundum")]
    public bool Orundum { get; set; }
}

public class RoguelikeTask : BaseTask
{
    public RoguelikeTask() => TaskType = TaskType.Roguelike;

    [JsonPropertyName("theme")]
    public RoguelikeTheme Theme { get; set; } = RoguelikeTheme.JieGarden;

    [JsonPropertyName("mode")]
    public RoguelikeMode Mode { get; set; } = RoguelikeMode.Exp;

    [JsonPropertyName("start_count")]
    public int StartCount { get; set; } = 999999;

    [JsonPropertyName("investment")]
    public bool Investment { get; set; } = true;

    [JsonPropertyName("invest_count")]
    public int InvestCount { get; set; } = 999;
}

public class ReclamationTask : BaseTask
{
    public ReclamationTask() => TaskType = TaskType.Reclamation;

    [JsonPropertyName("theme")]
    public ReclamationTheme Theme { get; set; } = ReclamationTheme.Tales;

    [JsonPropertyName("mode")]
    public ReclamationMode Mode { get; set; } = ReclamationMode.Archive;
}

public class CopilotTask : BaseTask
{
    public CopilotTask() => TaskType = TaskType.Copilot;

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;
}

public class CustomTask : BaseTask
{
    public CustomTask() => TaskType = TaskType.Custom;

    [JsonPropertyName("task_name")]
    public string TaskName { get; set; } = string.Empty;
}
