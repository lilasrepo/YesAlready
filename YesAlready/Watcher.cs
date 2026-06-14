using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using ECommons.EzHookManager;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace YesAlready;

public class Watcher : IDisposable
{
    private readonly Hook<AtkUnitBase.Delegates.FireCallback> _fireCallbackHook;

    private bool _wasDisableKeyPressed;
    private uint _lastTargetId;

    public string LastSeenDialogText { get; set; } = string.Empty;
    public string LastSeenOkText { get; set; } = string.Empty;
    public string LastSeenListSelection { get; set; } = string.Empty;
    public int LastSeenListIndex { get; set; }
    public string LastSeenListTarget { get; set; } = string.Empty;
    public (int Index, string Text)[] LastSeenListEntries { get; set; } = [];
    public string LastSeenTalkTarget { get; set; } = string.Empty;
    public string LastSeenNumericsText { get; set; } = string.Empty;
    public DateTime EscapeLastPressed { get; set; } = DateTime.MinValue;
    public string EscapeTargetName { get; set; } = string.Empty;
    public bool ForcedYesKeyPressed { get; set; }
    public bool ForcedTalkKeyPressed { get; set; }
    public bool DisableKeyPressed { get; set; }
    public LastListEntry? LastSelectedListEntry { get; set; } = new();

    public unsafe Watcher()
    {
        EzSignatureHelper.Initialize(this);
        _fireCallbackHook = Svc.Hook.HookFromAddress<AtkUnitBase.Delegates.FireCallback>((nint)AtkUnitBase.MemberFunctionPointers.FireCallback, FireCallbackDetour);
        Svc.Framework.Update += FrameworkUpdate;
    }

    public void Dispose()
    {
        _fireCallbackHook.Dispose();
        Svc.Framework.Update -= FrameworkUpdate;
    }

    public class LastListEntry
    {
        public uint TargetDataId { get; set; }
        public ListEntryNode? Node { get; set; }
    }

    private void FrameworkUpdate(IFramework framework)
    {
        if (!P.Active && !_wasDisableKeyPressed) return;
        DisableKeyPressed = C.DisableKey != VirtualKey.NO_KEY && Svc.KeyState[C.DisableKey];

        if (P.Active && DisableKeyPressed && !_wasDisableKeyPressed)
            C.Enabled = false;
        else if (!P.Active && !DisableKeyPressed && _wasDisableKeyPressed)
            C.Enabled = true;

        _wasDisableKeyPressed = DisableKeyPressed;

        ForcedYesKeyPressed = C.ForcedYesKey != VirtualKey.NO_KEY && Svc.KeyState[C.ForcedYesKey];

        ForcedTalkKeyPressed = C.ForcedTalkKey != VirtualKey.NO_KEY && C.SeparateForcedKeys && Svc.KeyState[C.ForcedTalkKey];

        if (Svc.KeyState[VirtualKey.ESCAPE])
        {
            EscapeLastPressed = DateTime.Now;

            var target = Svc.Targets.Target;
            EscapeTargetName = target != null ? target.Name.GetText() : string.Empty;
        }

        if (Svc.Targets.Target is { DataId: var id })
        {
            if (id != _lastTargetId)
                Service.Watcher.LastSelectedListEntry = null;
            _lastTargetId = id;
        }
        else
            Service.Watcher.LastSelectedListEntry = null;
    }

    private unsafe void FireCallbackDetour(AtkUnitBase* thisPtr, uint valueCount, AtkValue* values, bool close)
    {
        if (thisPtr->NameString is not ("SelectString" or "SelectIconString"))
        {
            _fireCallbackHook.Original(thisPtr, valueCount, values, close);
            return;
        }

        try
        {
            var atkValueList = Enumerable.Range(0, (int)valueCount)
                .Select<int, object>(i => values[i].Type switch
                {
                    FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int => values[i].Int,
                    FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String => Marshal.PtrToStringUTF8(new IntPtr(values[i].String)) ?? string.Empty,
                    FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt => values[i].UInt,
                    FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool => values[i].Byte != 0,
                    _ => $"Unknown Type: {values[i].Type}"
                })
                .ToList();
            PluginLog.Debug($"[{nameof(Watcher)}] Callback triggered on {thisPtr->NameString} with values: {string.Join(", ", atkValueList.Select(value => value.ToString()))}");
            LastSeenListIndex = values[0].Int;
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Exception in {nameof(FireCallbackDetour)}: {ex.Message}");
        }
        _fireCallbackHook.Original(thisPtr, valueCount, values, close);
    }
}
