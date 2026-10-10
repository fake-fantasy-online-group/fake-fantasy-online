using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public sealed class WorldHelpTopic
{
    public WorldHelpTopic(string title, string content)
    {
        Title = title ?? string.Empty;
        Content = content ?? string.Empty;
    }

    public string Title { get; }
    public string Content { get; }
}

public sealed class WorldHelpTopicsState
{
    public static readonly WorldHelpTopicsState Empty =
        new WorldHelpTopicsState(Array.Empty<WorldHelpTopic>());

    public WorldHelpTopicsState(IReadOnlyList<WorldHelpTopic> topics)
    {
        Topics = new List<WorldHelpTopic>(topics).AsReadOnly();
    }

    public IReadOnlyList<WorldHelpTopic> Topics { get; }
}

public interface IHelpTopicsStateSource
{
    WorldHelpTopicsState HelpTopicsState { get; }
    long HelpTopicsStateVersion { get; }
}
}
