using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class TaskDB
{
    // Official 1.5.8 token 06000e78.
    public static IEnumerable<Def> All()
    {
        Ensure();
        if (_byId != null)
            return _byId.Values;
        return Array.Empty<Def>();
    }
}

}
