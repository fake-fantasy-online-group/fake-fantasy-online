using System;
using System.Globalization;

namespace FakeFantasyOnline.Presentation
{

/// <summary>
/// Decision-free opt-in entry that reaches the production first-world window and then waits for
/// user-owned foreground input. It never posts input or calls a dialog/task handler directly.
/// </summary>
internal static class ReferenceNpcTaskDialogueManualCheck
{
    public const string Argument = "--ffo-npc-task-dialogue-manual-check";
    public const string Marker = "FFO_NPC_TASK_DIALOGUE_MANUAL_1_5_8_V1";

    public static bool IsRequested(string[] arguments)
    {
        if (arguments == null)
        {
            return false;
        }

        for (int index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(arguments[index], Argument, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string ReadyLine(int mapId, int npcCount, int entityCount)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} READY state=InWorld map={1} npcs={2} entities={3} input=manual-only",
            Marker,
            mapId,
            Math.Max(0, npcCount),
            Math.Max(0, entityCount));
    }

    public static string StageLine(string stage, string npcName)
    {
        return Marker + " STAGE " + (stage ?? string.Empty) + " npc=" +
               (npcName ?? string.Empty);
    }
}
}
