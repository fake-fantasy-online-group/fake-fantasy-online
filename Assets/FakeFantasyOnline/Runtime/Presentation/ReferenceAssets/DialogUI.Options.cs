using System;
using System.Collections;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    // 0600021e: native storage is deliberately non-transactional, labels first.
    private void Add(string label, Action action)
    {
        optLabels.Add(label);
        optActs.Add(action);
    }

    private void Add(ReferenceNpcDialogOption option)
    {
        Action action = option.BindTo(this);
        Add(option.Label, action);
    }

    // This lazy observation view owns no row or metadata storage. Rendering reads
    // optLabels and activation invokes optActs directly; both are the native lists.
    private sealed class OptionView : IReadOnlyList<ReferenceNpcDialogOption>
    {
        private readonly DialogUI owner;
        internal OptionView(DialogUI owner) { this.owner = owner; }
        public int Count => owner.optLabels.Count;
        public ReferenceNpcDialogOption this[int index] =>
            (ReferenceNpcDialogOption)owner.optActs[index].Target;

        public IEnumerator<ReferenceNpcDialogOption> GetEnumerator() =>
            new OptionEnumerator(this, owner.optLabels.GetEnumerator());
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // Capture the labels' List enumerator at GetEnumerator time so mutation still
    // invalidates enumeration, including mutation before the first MoveNext.
    private sealed class OptionEnumerator : IEnumerator<ReferenceNpcDialogOption>
    {
        private readonly OptionView view;
        private readonly IEnumerator<string> labels;
        private int index = -1;
        private ReferenceNpcDialogOption current;

        internal OptionEnumerator(OptionView view, IEnumerator<string> labels)
        {
            this.view = view;
            this.labels = labels;
        }

        public ReferenceNpcDialogOption Current => current;
        object IEnumerator.Current
        {
            get
            {
                // Preserve the List enumerator's before-start/after-end checks.
                _ = ((IEnumerator)labels).Current;
                return current;
            }
        }

        public bool MoveNext()
        {
            if (labels.MoveNext())
            {
                current = view[++index];
                return true;
            }
            current = null;
            return false;
        }

        public void Reset()
        {
            labels.Reset();
            index = -1;
            current = null;
        }

        public void Dispose() => labels.Dispose();
    }
}
}
