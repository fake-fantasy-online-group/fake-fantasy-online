using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal partial class ChatUI
    {
        public static Action<string, string> OnSend;

        // Official 0x060000de. Preserve body text; only extract references still present in it.
        public static void TakeOutgoing(string text, List<(byte, byte, int, string)> refs)
        {
            var located = ChatShareTokens.Locate(text, refs);
            located.Sort((a, b) => a.start.CompareTo(b.start));
            OutgoingShares = new List<(byte, byte, int)>();
            foreach (var token in located)
            {
                var reference = refs[token.index];
                OutgoingShares.Add((reference.Item1, reference.Item2, reference.Item3));
            }
            refs.Clear();
        }

        // Official 0x060000df is the direct tail-call to 0x060000da.
        public static string GuardShareEdit(string oldText, string newText, List<(byte, byte, int, string)> refs)
            => ChatShareTokens.Guard(oldText, newText, refs);
    }
}
