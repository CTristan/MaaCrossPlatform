using System.Collections.Generic;

namespace MaaGui.Helper;

public static class DataHelper
{
    public static readonly Dictionary<string, string> ClientDirectoryMapper = new()
    {
        { "zh-tw", "txwy" },
        { "en-us", "YoStarEN" },
        { "ja-jp", "YoStarJP" },
        { "ko-kr", "YoStarKR" },
    };
}
