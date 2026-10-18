using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class TaskClient
{
    // Official TaskClient+Offer. The official constructor only stores the empty string into the
    // three string fields before the base call, so those are declared as initializers here; the
    // generated constructor is the 06000e72 body and cannot be written as a named member.
    public class Offer
    {
        public int id;
        public byte state;
        public string title = "";
        public string desc = "";
        public string award = "";
    }

    // Official TaskClient static +0x10 / +0x18 / +0x28. OfferNpc keeps the "" initializer the
    // official .cctor (06000e70) stores, and Offers keeps the List allocation it performs.
    private static string OfferNpc = "";
    public static readonly List<Offer> Offers = new List<Offer>();
    // A single Action<string> slot, not a multicast event: the only official writer is
    // DialogUI.Show (0x06000211), which overwrites it with one plain store (arm64
    // 06000211.c:163-165, x86_64 :155-156), and SetOffers (0x06000e5e) invokes it once per
    // received packet. Do not turn this into an event or subscribe to it with +=.
    public static Action<string> OnOffersChanged;

    // The read side of the official +0x10 slot. DialogUI.BuildMenu (0x06000215) compares it with
    // the open NPC name before it walks TaskClient.Offers, so a reply for another NPC leaves the
    // rows of the open dialog empty instead of showing the previous NPC's offers.
    public static string OfferNpcName => OfferNpc;

    // Official 1.5.8 token 06000e5e. The official body stores the npc name, clears and refills the
    // offer list, then fires OnOffersChanged with that same name; a null Offers list throws.
    public static void SetOffers(string npc, List<Offer> offers)
    {
        OfferNpc = npc;
        Offers.Clear();
        Offers.AddRange(offers);
        OnOffersChanged?.Invoke(npc);
    }

    // Product projection of the same official body: the receive path only holds the protocol-level
    // replacement, so the official rows are allocated here and handed to SetOffers unchanged. No
    // row is filtered, reordered or de-duplicated and no default NPC is substituted.
    public static void SetWorldOffers(
        string npc,
        IReadOnlyList<WorldNpcTaskOfferState> offers)
    {
        List<Offer> rows = new List<Offer>(offers == null ? 0 : offers.Count);
        if (offers != null)
        {
            for (int index = 0; index < offers.Count; index++)
            {
                WorldNpcTaskOfferState row = offers[index];
                rows.Add(
                    new Offer
                    {
                        id = row.TaskId,
                        state = row.State,
                        title = row.Title,
                        desc = row.Description,
                        award = row.Award
                    });
            }
        }

        SetOffers(npc, rows);
    }

    // Official 1.5.8 tokens 06000e6c-06000e6f. All four are the same two-hop shape the official
    // uses elsewhere: a Unity op_Inequality liveness test on the NetClient singleton, then a single
    // forward of the argument. This tree keeps the session port as the live caller, so these
    // members are the source-verified official owners of the same four requests (0x1032-0x1035).
    public static void RequestOffers(string npc)
    {
        if (NetClient.I != null) NetClient.I.TaskNpcListForIndependentRuntime(npc);
    }

    public static void Accept(int id)
    {
        if (NetClient.I != null) NetClient.I.TaskAcceptForIndependentRuntime(id);
    }

    public static void Abandon(int id)
    {
        if (NetClient.I != null) NetClient.I.TaskAbandonForIndependentRuntime(id);
    }

    public static void Complete(int id)
    {
        if (NetClient.I != null)
        {
            DialogUI.I?.TrackTaskCompletion(id);
            NetClient.I.TaskCompleteForIndependentRuntime(id);
        }
    }
}

}
