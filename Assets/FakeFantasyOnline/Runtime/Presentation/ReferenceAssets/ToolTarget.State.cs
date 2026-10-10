using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class ToolTarget
{
    public static bool Active;
    private static int toolId;
    // Official 0x06000f44.
    public static int ToolId { get => toolId; private set => toolId = value; }
    public static string Verb => ResolveVerb(ToolId);
    public static string Warn => ResolveWarn(ToolId);

    // Official 1.5.8 0x06000f48.
    public static void Begin(int toolId)
    {
        ToolId = toolId;
        Active = true;
        CursorManager.I?.SetAvatarMode(true);
        EquipDlg.EnsureOpen();
    }

    // Equipment lifecycle is supplied by the independently constructed window owner.
    internal static void BeginForIndependentRuntime(int toolId, Action ensureEquipmentOpen)
    {
        ToolId = toolId;
        Active = true;
        CursorManager.I?.SetAvatarMode(true);
        ensureEquipmentOpen();
    }

    public static void Cancel()
    {
        if (!Active) return;
        Active = false;
        CursorManager.I?.SetAvatarMode(false);
    }

    internal static string ResolveVerb(int toolId)
    {
        switch (toolId)
        {
            case 0x3125: return "分离变装";
            case 0x6311: return "解除绑定";
            case 0x182ba: return "分离灵外观";
            case 0x182bb: return "分离龙之元素";
            case 0x182bc: return "取出一张卡片";
            case 0x182ce:
            case 0x182d4:
            case 0x182d5:
            case 0x182d6:
            case 0x182d7: return "品质洗炼";
            case 0x182cf: return "还原初始属性";
            default: return "使用";
        }
    }

    internal static string ResolveWarn(int toolId)
    {
        if (toolId == 0x182cf)
            return "\n注意: 孔里的符文卡会全部取下退回背包\n(孔数保留; 背包空位不够则整单取消)";
        if (toolId == 0x182bc)
            return "\n注意: 每次只取【第一个有卡的孔】那一张\n(孔数保留; 背包不足1格则整单取消)";
        return string.Empty;
    }
}
}
