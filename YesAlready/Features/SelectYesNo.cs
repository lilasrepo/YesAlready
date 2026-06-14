using Dalamud.Game.Text;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace YesAlready.Features;

[AddonFeature(AddonEvent.PostSetup)]
internal class SelectYesno : TextMatchingFeature
{
    protected override unsafe string GetSetLastSeenText(AtkUnitBase* atk)
    {
        var text = new AddonMaster.SelectYesno(atk).TextLegacy;
        Service.Watcher.LastSeenDialogText = text;
        return text;
    }

    protected override unsafe object? ShouldProceed(string text, AtkUnitBase* atk)
    {
        if (Service.Watcher.ForcedYesKeyPressed)
        {
            Log($"Forced yes hotkey pressed");
            return new TextEntryNode { IsYes = true };
        }

        // TODO(api12): GimmickYesNo.Message is game-7.5 only; B1-stub the gimmick check
        // if (C.GimmickYesNo && Svc.Data.GetExcelSheet<GimmickYesNo>().Where(x => !x.Message.IsEmpty).Select(x => x.Message).ToList().Any(g => g.EqualsIgnoreSpecial(text)))
        // {
        //     Log($"Entry is a gimmick");
        //     return new TextEntryNode { IsYes = true };
        // }

        if (C.PartyFinderJoinConfirm && GenericHelpers.TryGetAddonByName<AtkUnitBase>("LookingForGroupDetail", out var _) && lfgPatterns.Any(r => r.IsMatch(text)))
        {
            Log($"Entry is party finder join confirmation");
            return new TextEntryNode { IsYes = true };
        }

        // TODO(api12): AutoCollectable disabled — depends on GenericHelpers.FindSubrow (ECommons HEAD-only),
        // WKSItemInfo Lumina sheet (game-7.5), Item.AdditionalData wrapper (Lumina sub-row drift).
        // B1 stub: skip auto-collectable handling entirely.
        // if (C.AutoCollectable && collectablePatterns.Any(text.Contains)) { ... }

        var nodes = C.GetAllNodes().OfType<TextEntryNode>();
        foreach (var node in nodes)
        {
            if (!node.Enabled || string.IsNullOrEmpty(node.Text))
                continue;

            if (!CheckRestrictions(node))
                continue;

            if (EntryMatchesText(node.Text, text, node.IsTextRegex))
                return node;
        }

        return null;
    }

    protected override unsafe void Proceed(AtkUnitBase* atk, object? matchingNode)
    {
        if (matchingNode is not TextEntryNode node) return;
        if (node.IsYes)
            new AddonMaster.SelectYesno(atk).Yes();
        else
            new AddonMaster.SelectYesno(atk).No();
    }

    private static readonly List<Regex> lfgPatterns =
    [
        new Regex(@"Join .* party\?"),
        new Regex(@".*のパーティに参加します。よろしいですか？"),
        new Regex(@"Der Gruppe von .* beitreten\?"),
        new Regex(@"Rejoindre l'équipe de .*\?")
        // if someone could add the chinese and korean translations that'd be nice
    ];

    private readonly List<string> collectablePatterns =
    [
        "collectability of",
        "収集価値",
        "Sammlerwert",
        "Valeur de collection"
        // if someone could add the chinese and korean translations that'd be nice
    ];
}
