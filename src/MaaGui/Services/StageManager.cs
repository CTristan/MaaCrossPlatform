using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Threading;
using MaaGui.Configuration;
using MaaGui.Helper;
using MaaGui.Localization;
using MaaGui.Models;
using MaaGui.Services.MaaInterop;
using MaaGui.Services.Notification;
using MaaGui.Services.Web;
using Semver;
using Serilog;

namespace MaaGui.Services;

/// <summary>
/// Stage manager
/// </summary>
public class StageManager
{
    private const string StageApi = "gui/StageActivityV2.json";
    private const string TasksApi = "resource/tasks.json";

    private static readonly ILogger _logger = Log.ForContext<StageManager>();

    private readonly IMaaApiService _maaApiService;
    private readonly AsstProxy _asstProxy;
    private readonly INotificationPoster _notificationPoster;
    private readonly Root _config;

    // data
    private Dictionary<string, StageInfo> _stages = new();
    private List<MiniGameEntry> _miniGameEntries = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StageManager"/> class.
    /// </summary>
    public StageManager(IMaaApiService maaApiService, AsstProxy asstProxy, INotificationPoster notificationPoster, Root config)
    {
        _maaApiService = maaApiService;
        _asstProxy = asstProxy;
        _notificationPoster = notificationPoster;
        _config = config;

        _miniGameEntries = InitializeDefaultMiniGameEntries();
        UpdateStageLocal();
    }

    /// <summary>
    /// Gets mini game entries
    /// </summary>
    public IReadOnlyList<MiniGameEntry> MiniGameEntries => _miniGameEntries.AsReadOnly();

    public void UpdateStageLocal()
    {
        MergePermanentAndActivityStages(LoadLocalStages());
    }

    public async Task UpdateStageWeb()
    {
        try
        {
            var webStages = await LoadWebStages();
            if (webStages is null)
            {
                return;
            }

            MergePermanentAndActivityStages(webStages);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _notificationPoster.Post(LocalizationHelper.GetString("ApiUpdateSuccess"), "Information");
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update stages from web");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _notificationPoster.Post(LocalizationHelper.GetString("ApiUpdateFailed"), "Error");
            });
        }
    }

    private string GetClientType()
    {
        var clientType = _config.GameSettings.ClientType;

        // Default to Official
        if (string.IsNullOrEmpty(clientType) || clientType == "Bilibili")
        {
            clientType = "Official";
        }

        return clientType;
    }

    private JsonNode? LoadLocalStages()
    {
        return _maaApiService.LoadApiCache(StageApi);
    }

    private async Task<JsonNode?> LoadWebStages()
    {
        try
        {
            var clientType = GetClientType();

            var activityTask = _maaApiService.RequestMaaApiWithCache(StageApi, false);
            var tasksTask = _maaApiService.RequestMaaApiWithCache(TasksApi, false);

            await Task.WhenAll(activityTask, tasksTask);

            var activityJson = await activityTask;
            var tasksJson = await tasksTask;

            JsonNode? globalTasksJson = null;
            if (clientType != "Official" && tasksJson != null)
            {
                var tasksPath = "resource/global/" + clientType + '/' + TasksApi;
                globalTasksJson = await _maaApiService.RequestMaaApiWithCache(tasksPath, false);
            }

            if (activityJson is null || tasksJson is null)
            {
                return null;
            }

            if (clientType != "Official" && globalTasksJson is null)
            {
                return null;
            }

            // Trigger resource load in core
            _ = Task.Run(async () =>
            {
                try
                {
                    await _asstProxy.LoadResourceWhenIdleAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error loading resources when idle");
                }
            });

            return activityJson;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading web stages");
            return null;
        }
    }

    private void MergePermanentAndActivityStages(JsonNode? activity)
    {
        var tempStage = InitializeDefaultStages();
        var clientType = GetClientType();

        // Version check logic (simplified for Phase 1)
        bool isDebugVersion = true; // Assume debug for now or implement version check
        string coreVersion = "5.0.0"; // Placeholder
        bool curVerParsed = SemVersion.TryParse(coreVersion, SemVersionStyles.AllowLowerV, out var curVersionObj);

        var resourceCollection = InitializeResourceCollection(activity?[clientType]?["resourceCollection"]);

        if (activity?[clientType] != null)
        {
            ParseActivityStages(activity[clientType], tempStage, curVerParsed, curVersionObj, isDebugVersion);
        }

        AddPermanentStages(tempStage, resourceCollection);

        _stages = tempStage;

        // Parse mini-game tasks
        var tempMiniGames = InitializeDefaultMiniGameEntries();
        ParseMiniGameEntries(activity?[clientType], tempMiniGames, curVerParsed, curVersionObj, isDebugVersion);
        _miniGameEntries = tempMiniGames;
    }

    private static Dictionary<string, StageInfo> InitializeDefaultStages()
    {
        return new()
        {
            { string.Empty, new() { Display = LocalizationHelper.GetString("DefaultStage"), Value = string.Empty } },
            { "Pormpt1", new() { Tip = LocalizationHelper.GetString("Pormpt1"), OpenDaysOfWeek = new[] { DayOfWeek.Monday }, IsHidden = true } },
            { "Pormpt2", new() { Tip = LocalizationHelper.GetString("Pormpt2"), OpenDaysOfWeek = new[] { DayOfWeek.Sunday }, IsHidden = true } },
        };
    }

    private static List<MiniGameEntry> InitializeDefaultMiniGameEntries()
    {
        return new List<MiniGameEntry>
        {
            new() { Display = LocalizationHelper.GetString("MiniGameNameSsStore"), Value = "SS@Store@Begin", TipKey = "MiniGameNameSsStoreTip" },
            new() { Display = LocalizationHelper.GetString("MiniGameNameGreenTicketStore"), Value = "GreenTicket@Store@Begin", TipKey = "MiniGameNameGreenTicketStoreTip" },
            new() { Display = LocalizationHelper.GetString("MiniGameNameYellowTicketStore"), Value = "YellowTicket@Store@Begin", TipKey = "MiniGameNameYellowTicketStoreTip" },
            new() { Display = LocalizationHelper.GetString("MiniGameNameRAStore"), Value = "RA@Store@Begin", TipKey = "MiniGameNameRAStoreTip" },
            new() { Display = LocalizationHelper.GetString("MiniGame@SecretFront"), Value = "MiniGame@SecretFront", TipKey = "MiniGame@SecretFrontTip" },
        };
    }

    private static StageActivityInfo InitializeResourceCollection(JsonNode? resourceCollectionData)
    {
        if (resourceCollectionData == null)
        {
            return new() { IsResourceCollection = true };
        }

        return new()
        {
            IsResourceCollection = true,
            Tip = resourceCollectionData["Tip"]?.ToString(),
            UtcStartTime = ParseDateTime(resourceCollectionData, "UtcStartTime"),
            UtcExpireTime = ParseDateTime(resourceCollectionData, "UtcExpireTime"),
        };
    }

    private static DateTime ParseDateTime(JsonNode? token, string key)
    {
        if (token == null || token[key] == null) return DateTime.MinValue;

        if (DateTime.TryParseExact(token[key]?.ToString() ?? string.Empty, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            var timeZone = 0;
            if (token["TimeZone"] != null)
            {
                timeZone = token["TimeZone"].GetValue<int>();
            }
            return dt.AddHours(-timeZone);
        }
        return DateTime.MinValue;
    }

    private void ParseActivityStages(JsonNode? clientData, Dictionary<string, StageInfo> tempStage, bool curVerParsed, SemVersion? curVersionObj, bool isDebugVersion)
    {
        try
        {
            var sideToken = clientData?["sideStoryStage"];
            if (sideToken is not JsonObject sideObject)
            {
                return;
            }

            foreach (var kvp in sideObject)
            {
                var group = kvp.Value;
                if (group == null) continue;

                JsonNode? activityToken = group["Activity"] ?? group["activity"];
                var groupMinReqStr = group["MinimumRequired"]?.ToString() ?? group["minimumRequired"]?.ToString();

                JsonNode? stagesToken = group["Stages"] ?? group["stages"];
                if (stagesToken is not JsonArray stagesArray) continue;

                foreach (var stageObj in stagesArray)
                {
                    var minReqStr = stageObj?["MinimumRequired"]?.ToString() ?? groupMinReqStr;
                    SemVersion? minRequiredObj = null;
                    bool versionOk = string.IsNullOrEmpty(minReqStr) || SemVersion.TryParse(minReqStr, SemVersionStyles.AllowLowerV, out minRequiredObj);

                    bool unsupportedStages = !isDebugVersion && curVerParsed && versionOk && minRequiredObj != null && curVersionObj!.CompareSortOrderTo(minRequiredObj) < 0;

                    var stageInfo = CreateStageInfo(stageObj, unsupportedStages, minRequiredObj, activityToken);
                    tempStage.TryAdd(stageInfo.Display, stageInfo);
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to parse activity stages");
        }
    }

    private static StageInfo CreateStageInfo(JsonNode? stageObj, bool unsupportedStages, SemVersion? minRequiredObj, JsonNode? activityToken)
    {
        activityToken = stageObj?["Activity"] ?? activityToken;

        var display = stageObj?["Display"]?.ToString() ?? string.Empty;
        var value = stageObj?["Value"]?.ToString() ?? string.Empty;

        var drop = unsupportedStages
            ? LocalizationHelper.GetString("LowVersion") + "\n" + LocalizationHelper.GetString("MinimumRequirements") + minRequiredObj
            : stageObj?["Drop"]?.ToString();

        var activity = new StageActivityInfo
        {
            Tip = activityToken?["Tip"]?.ToString(),
            StageName = activityToken?["StageName"]?.ToString(),
            UtcStartTime = ParseDateTime(activityToken, "UtcStartTime"),
            UtcExpireTime = ParseDateTime(activityToken, "UtcExpireTime"),
        };

        return new StageInfo(display, value, drop, activity);
    }

    private void ParseMiniGameEntries(JsonNode? clientData, List<MiniGameEntry> tempMiniGames, bool curVerParsed, SemVersion? curVersionObj, bool isDebugVersion)
    {
        if (clientData == null) return;

        var miniGameToken = clientData["miniGame"];
        if (miniGameToken == null) return;

        var parsedEntries = new List<MiniGameEntry>();

        void ProcessItem(JsonNode? item)
        {
            var entry = ParseMiniGameEntry(item);
            if (entry == null) return;

            if (!string.IsNullOrEmpty(entry.MinimumRequired))
            {
                if (SemVersion.TryParse(entry.MinimumRequired, SemVersionStyles.AllowLowerV, out var minReqObj))
                {
                    bool unsupported = !isDebugVersion && curVerParsed && curVersionObj!.CompareSortOrderTo(minReqObj) < 0;
                    if (unsupported)
                    {
                        entry.Tip = LocalizationHelper.GetString("LowVersion") + "\n" + LocalizationHelper.GetString("MinimumRequirements") + " " + entry.MinimumRequired;
                        entry.TipKey = null;
                    }
                }
            }

            if (entry.BeingOpen) parsedEntries.Add(entry);
        }

        if (miniGameToken is JsonArray miniGameArray)
        {
            foreach (var item in miniGameArray) ProcessItem(item);
        }
        else
        {
            ProcessItem(miniGameToken);
        }

        if (parsedEntries.Count > 0)
        {
            tempMiniGames.InsertRange(0, parsedEntries);
        }
    }

    private static MiniGameEntry? ParseMiniGameEntry(JsonNode? token)
    {
        if (token == null) return null;

        if (token is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var val))
        {
            return new MiniGameEntry { Display = val, Value = val };
        }

        var entry = new MiniGameEntry
        {
            Display = token["Display"]?.ToString() ?? token["DisplayKey"]?.ToString() ?? token["Value"]?.ToString() ?? string.Empty,
            DisplayKey = token["DisplayKey"]?.ToString(),
            Value = token["Value"]?.ToString() ?? token["value"]?.ToString() ?? string.Empty,
            Tip = token["Tip"]?.ToString(),
            TipKey = token["TipKey"]?.ToString(),
            MinimumRequired = token["MinimumRequired"]?.ToString(),
            UtcStartTime = ParseDateTime(token, "UtcStartTime"),
            UtcExpireTime = ParseDateTime(token, "UtcExpireTime")
        };

        if (string.IsNullOrEmpty(entry.Display) && !string.IsNullOrEmpty(entry.DisplayKey))
        {
            entry.Display = LocalizationHelper.GetString(entry.DisplayKey);
        }

        return entry;
    }

    private void AddPermanentStages(Dictionary<string, StageInfo> tempStage, StageActivityInfo resourceCollection)
    {
        var permanentStages = new Dictionary<string, StageInfo>
        {
            { "1-7", new() { Display = "1-7", Value = "1-7" } },
            { "R8-11", new() { Display = "R8-11", Value = "R8-11" } },
            { "12-17-HARD", new() { Display = "12-17-HARD", Value = "12-17-HARD" } },

            { "CE-6", new("CE-6", "CETip", new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday }, resourceCollection) },
            { "AP-5", new("AP-5", "APTip", new[] { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday }, resourceCollection) },
            { "CA-5", new("CA-5", "CATip", new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday }, resourceCollection) },
            { "LS-6", new("LS-6", "LSTip", Array.Empty<DayOfWeek>(), resourceCollection) },
            { "SK-5", new("SK-5", "SKTip", new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Saturday }, resourceCollection) },

            { "Annihilation", new() { Display = LocalizationHelper.GetString("AnnihilationMode"), Value = "Annihilation" } },

            { "PR-A-1", new("PR-A-1", "PR-ATip", new[] { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday }, resourceCollection, new List<List<string>> { new() { "3231", "3261" }, new() { "3232", "3262" } }) },
            { "PR-A-2", new("PR-A-2", string.Empty, new[] { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday }, resourceCollection) },
            { "PR-B-1", new("PR-B-1", "PR-BTip", new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Friday, DayOfWeek.Saturday }, resourceCollection, new List<List<string>> { new() { "3251", "3241" }, new() { "3252", "3242" } }) },
            { "PR-B-2", new("PR-B-2", string.Empty, new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Friday, DayOfWeek.Saturday }, resourceCollection) },
            { "PR-C-1", new("PR-C-1", "PR-CTip", new[] { DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday }, resourceCollection, new List<List<string>> { new() { "3211", "3271" }, new() { "3212", "3272" } }) },
            { "PR-C-2", new("PR-C-2", string.Empty, new[] { DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday }, resourceCollection) },
            { "PR-D-1", new("PR-D-1", "PR-DTip", new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Saturday, DayOfWeek.Sunday }, resourceCollection, new List<List<string>> { new() { "3221", "3281" }, new() { "3222", "3282" } }) },
            { "PR-D-2", new("PR-D-2", string.Empty, new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Saturday, DayOfWeek.Sunday }, resourceCollection) },
        };

        foreach (var kvp in permanentStages)
        {
            tempStage.TryAdd(kvp.Key, kvp.Value);
        }
    }

    public StageInfo GetStageInfo(string stage)
    {
        if (_stages.TryGetValue(stage, out var stageInfo)) return stageInfo;

        if (!string.IsNullOrEmpty(stage) && Regex.IsMatch(stage, "^[A-Za-z]{2}-\\d{1,2}$"))
        {
            return new StageInfo(stage, stage, null, new StageActivityInfo());
        }

        return new StageInfo { Display = stage, Value = stage };
    }

    public string GetStageTips(DayOfWeek dayOfWeek)
    {
        var lines = new List<string>();
        bool resourceTipShown = false;
        DateTime now = DateTime.UtcNow;

        foreach (var stage in _stages.Values.Where(s => s.IsStageOpen(dayOfWeek)))
        {
            var activity = stage.Activity;

            if (!resourceTipShown && activity is { IsResourceCollection: true, BeingOpen: true })
            {
                lines.Insert(0, $"｢{activity.Tip}｣ {LocalizationHelper.GetString("DaysLeftOpen")}{GetDaysLeftText(activity.UtcExpireTime, now)}");
                resourceTipShown = true;
            }

            if (!string.IsNullOrEmpty(activity?.StageName))
            {
                lines.Add($"｢{activity.StageName}｣ {LocalizationHelper.GetString("DaysLeftOpen")}{GetDaysLeftText(activity.UtcExpireTime, now)}");
            }

            if (!string.IsNullOrEmpty(stage.Drop))
            {
                lines.Add($"{stage.Value}: {ItemListHelper.GetItemName(stage.Drop) ?? stage.Drop}");
            }

            if (!string.IsNullOrEmpty(stage.Tip))
            {
                lines.Add(stage.Tip);
            }
        }

        return lines.Count > 0 ? string.Join(Environment.NewLine, lines.Distinct()) : string.Empty;
    }

    private static string GetDaysLeftText(DateTime expireTime, DateTime now)
    {
        int daysLeft = (expireTime - now).Days;
        return daysLeft > 0 ? daysLeft.ToString() : LocalizationHelper.GetString("LessThanOneDay");
    }

    public IEnumerable<StageInfo> GetStageList(DayOfWeek dayOfWeek)
    {
        return _stages.Values.Where(stage => !stage.IsHidden && stage.IsStageOpen(dayOfWeek));
    }

    public IEnumerable<StageInfo> GetStageList()
    {
        return _stages.Values.Where(stage => !stage.IsHidden && stage.IsStageOpenOrWillOpen());
    }
}
