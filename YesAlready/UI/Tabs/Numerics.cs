using ImGuiNET;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using System.Numerics;
using System.Text;
using YesAlready.Interface;

namespace YesAlready.UI.Tabs;

public static class Numerics
{
    private static TextFolderNode NumericsRootFolder => C.NumericsRootFolder;

    public static void DrawButtons()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X / 2, ImGui.GetStyle().ItemSpacing.Y));

        if (ImGuiX.IconButton(FontAwesomeIcon.Plus, "Add new entry"))
        {
            var newNode = new NumericsEntryNode { Enabled = false, Text = "Your text goes here" };
            NumericsRootFolder.Children.Add(newNode);
            C.Save();
        }

        ImGui.SameLine();
        if (ImGuiX.IconButton(FontAwesomeIcon.SearchPlus, "Add last seen as new entry"))
        {
            var io = ImGui.GetIO();
            var createFolder = io.KeyShift;
            var zoneRestricted = io.KeyCtrl;

            Configuration.CreateNode<NumericsEntryNode>(NumericsRootFolder, createFolder, zoneRestricted ? Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>()!.GetRowOrDefault(Player.Territory)?.PlaceName.Value.Name.ToString() ?? string.Empty : null);
            C.Save();
        }

        ImGui.SameLine();
        if (ImGuiX.IconButton(FontAwesomeIcon.FolderPlus, "Add folder"))
        {
            var newNode = new TextFolderNode { Name = "Untitled folder" };
            NumericsRootFolder.Children.Add(newNode);
            C.Save();
        }

        var sb = new StringBuilder();
        sb.AppendLine("Enter into the input all or part of the text inside a dialog.");
        sb.AppendLine("For example: \"Remove how many from stack?\" for the split stack dialog.");
        sb.AppendLine();
        sb.AppendLine("Alternatively, wrap your text in forward slashes to use as a regex.");
        sb.AppendLine("As such: \"/Remove .*/\"");
        sb.AppendLine();
        sb.AppendLine("If it matches, the ok button will be clicked.");
        sb.AppendLine();
        sb.AppendLine("Right click a line to view options.");
        sb.AppendLine("Double click an entry for quick enable/disable.");
        sb.AppendLine("Ctrl-Shift right click a line to delete it and any children.");
        sb.AppendLine();
        sb.AppendLine("\"Add last seen as new entry\" button modifiers:");
        sb.AppendLine("   Shift-Click to add to a new or first existing folder.");
        sb.AppendLine("   Ctrl-Click to create an entry restricted to the current zone.");
        sb.AppendLine();
        sb.AppendLine("Currently supported numeric addons:");
        sb.AppendLine("  - InputNumeric");

        ImGui.SameLine();
        ImGuiX.IconButton(FontAwesomeIcon.QuestionCircle, sb.ToString());
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(sb.ToString());
    }

    public static void DrawPopup(NumericsEntryNode node, Vector2 spacing)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        var enabled = node.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            node.Enabled = enabled;
            C.Save();
        }

        var trashAltWidth = ImGuiX.GetIconButtonWidth(FontAwesomeIcon.TrashAlt);

        ImGui.SameLine(ImGui.GetContentRegionMax().X - trashAltWidth);
        if (ImGuiX.IconButton(FontAwesomeIcon.TrashAlt, "Delete"))
        {
            if (C.TryFindParent(node, out var parentNode))
            {
                parentNode!.Children.Remove(node);
                C.Save();
            }
        }

        var matchText = node.Text;
        if (ImGui.InputText($"##{node.Name}-matchText", ref matchText, 10_000, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
        {
            node.Text = matchText;
            C.Save();
        }

        var percent = node.IsPercent;
        if (ImGui.Checkbox("Percentage", ref percent))
        {
            node.IsPercent = percent;
            C.Save();
        }
        if (node.IsPercent)
        {
            var percentage = node.Percentage;
            if (ImGui.SliderInt($"Percent of Max##{node.GetHashCode()}", ref percentage, 0, 100, "%d%%", ImGuiSliderFlags.AlwaysClamp))
            {
                node.Percentage = percentage < 0 ? 0 : percentage;
                node.Percentage = percentage > 100 ? 100 : percentage;
                C.Save();
            }
        }
        else
        {
            var quantity = node.Quantity;
            if (ImGui.InputInt($"Default Quantity##{node.GetHashCode()}", ref quantity))
            {
                node.Quantity = quantity < 1 ? 1 : quantity;
                C.Save();
            }
        }

        var zoneRestricted = node.ZoneRestricted;
        if (ImGui.Checkbox("Zone Restricted", ref zoneRestricted))
        {
            node.ZoneRestricted = zoneRestricted;
            C.Save();
        }

        var searchWidth = ImGuiX.GetIconButtonWidth(FontAwesomeIcon.Search);
        var searchPlusWidth = ImGuiX.GetIconButtonWidth(FontAwesomeIcon.SearchPlus);

        ImGui.SameLine(ImGui.GetContentRegionMax().X - searchWidth);
        if (ImGuiX.IconButton(FontAwesomeIcon.Search, "Zone List"))
            EzConfigGui.GetWindow<ZoneListWindow>()?.Toggle();

        ImGui.SameLine(ImGui.GetContentRegionMax().X - searchWidth - searchPlusWidth - spacing.X);
        if (ImGuiX.IconButton(FontAwesomeIcon.SearchPlus, "Fill with current zone"))
        {
            var currentID = Svc.ClientState.TerritoryType;
            if (P.TerritoryNames.TryGetValue(currentID, out var zoneName))
            {
                node.ZoneText = zoneName;
                C.Save();
            }
            else
            {
                node.ZoneText = "Could not find name";
                C.Save();
            }
        }

        var zoneText = node.ZoneText;
        if (ImGui.InputText($"##{node.Name}-zoneText", ref zoneText, 10_000, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
        {
            node.ZoneText = zoneText;
            C.Save();
        }

        //var targetRestricted = node.TargetRestricted;
        //if (ImGui.Checkbox("Target Restricted", ref targetRestricted))
        //{
        //    node.TargetRestricted = targetRestricted;
        //    C.Save();
        //}

        //var searchPlusWidth = Utils.ImGuiEx.GetIconButtonWidth(FontAwesomeIcon.SearchPlus);

        //ImGui.SameLine(ImGui.GetContentRegionMax().X - searchPlusWidth);
        //if (Utils.ImGuiEx.IconButton(FontAwesomeIcon.SearchPlus, "Fill with current target"))
        //{
        //    var target = Svc.Targets.Target;
        //    var name = target?.Name?.TextValue ?? string.Empty;

        //    if (!string.IsNullOrEmpty(name))
        //    {
        //        node.TargetText = name;
        //        C.Save();
        //    }
        //    else
        //    {
        //        node.TargetText = "Could not find target";
        //        C.Save();
        //    }
        //}

        //var targetText = node.TargetText;
        //if (ImGui.InputText($"##{node.Name}-targetText", ref targetText, 10_000, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputFlags.EnterReturnsTrue))
        //{
        //    node.TargetText = targetText;
        //    C.Save();
        //}
    }

    public static void DisplayEntryNode(NumericsEntryNode node)
    {
        var validRegex = node.IsTextRegex && node.TextRegex != null || !node.IsTextRegex;
        var validZone = !node.ZoneRestricted || node.ZoneIsRegex && node.ZoneRegex != null || !node.ZoneIsRegex;

        if (!node.Enabled && (!validRegex || !validZone))
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(.5f, 0, 0, 1));
        else if (!node.Enabled)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(.5f, .5f, .5f, 1));
        else if (!validRegex || !validZone)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));

        ImGui.TreeNodeEx($"{node.Name}##{node.Name}-tree", ImGuiTreeNodeFlags.Leaf);
        ImGui.TreePop();

        if (!node.Enabled || !validRegex || !validZone)
            ImGui.PopStyleColor();

        if (!validRegex && !validZone)
            ImGuiX.TextTooltip("Invalid Text and Zone Regex");
        else if (!validRegex)
            ImGuiX.TextTooltip("Invalid Text Regex");
        else if (!validZone)
            ImGuiX.TextTooltip("Invalid Zone Regex");

        if (ImGui.IsItemHovered())
        {
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                node.Enabled = !node.Enabled;
                C.Save();
                return;
            }
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                var io = ImGui.GetIO();
                if (io.KeyCtrl && io.KeyShift)
                {
                    if (C.TryFindParent(node, out var parent))
                    {
                        parent!.Children.Remove(node);
                        C.Save();
                    }

                    return;
                }
                else
                {
                    ImGui.OpenPopup($"{node.GetHashCode()}-popup");
                }
            }
        }

        MainWindow.TextNodePopup(node);
        MainWindow.TextNodeDragDrop(node);
    }
}
