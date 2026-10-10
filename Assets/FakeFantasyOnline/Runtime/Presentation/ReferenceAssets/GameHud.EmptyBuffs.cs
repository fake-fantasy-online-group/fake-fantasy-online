using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud : MonoBehaviour
{
    // Declaration and native ordering are in GameHud.Initialization.cs.
    // Official GameHud static <c>EmptyBuffs</c>: field token 67110362, <c>List&lt;CharData.BuffView&gt;</c>,
    // statics +0xa8, flags 0x31. One writer, <c>GameHud..cctor</c> (0x06000455, arm64 0x443294 /
    // x86_64 0x45b19b); one reader, <c>GameHud.DrawBuffBar</c> (0x06000449, arm64 0x438a68 /
    // x86_64 0x44fdce). The 90-member +0xa8 scan separating it from the instance-field
    // look-alikes is in Build/WBB-110-report.md. <c>internal</c> only because the DrawBuffBar
    // body is hosted in <see cref="ReferenceFirstWorldBuffHud"/>.
}

}
