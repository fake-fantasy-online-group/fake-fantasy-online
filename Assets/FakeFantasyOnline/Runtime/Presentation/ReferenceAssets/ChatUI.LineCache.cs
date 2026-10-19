using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // All history writes go through Add/Clear. Keep this separate from the public
    // Ver counter, whose existing clear/history-window behavior is unchanged.
    private static int lineContentRevision;

    private readonly struct LineCacheKey : IEquatable<LineCacheKey>
    {
        private readonly int revision, count, hidden;
        private readonly string character;

        internal LineCacheKey(int revision, int count, int hidden, string character)
        {
            this.revision = revision;
            this.count = count;
            this.hidden = hidden;
            this.character = character;
        }

        public bool Equals(LineCacheKey other) => revision == other.revision &&
            count == other.count && hidden == other.hidden && character == other.character;
    }

    private static LineCacheKey CurrentLineCacheKey =>
        new LineCacheKey(lineContentRevision, chatLines.Count, filterHide, _filterChar);
}
}
