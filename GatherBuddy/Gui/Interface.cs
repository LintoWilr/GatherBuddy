﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using GatherBuddy.Time;
using ImGuiNET;
using Functions = GatherBuddy.Plugin.Functions;
using ImRaii = OtterGui.Raii.ImRaii;

namespace GatherBuddy.Gui;

public partial class Interface : Window, IDisposable
{
    private const string PluginName = "GatherBuddy";
    private const float  MinSize    = 700;

    public static GatherBuddy Plugin                 = null!;
    private        TimeStamp   _earliestKeyboardToggle = TimeStamp.Epoch;

    private static List<ExtendedFish>? _extendedFishList;

    public static IReadOnlyList<ExtendedFish> ExtendedFishList
        => _extendedFishList ??= GatherBuddy.GameData.Fishes.Values
            .Where(f => f.FishingSpots.Count > 0 && f.InLog)
            .Select(f => new ExtendedFish(f)).ToList();

    public Interface(GatherBuddy plugin)
        : base(GatherBuddy.Version.Length > 0 ? $"{PluginName} v{GatherBuddy.Version}###GatherBuddyMain" : PluginName)
    {
        Plugin            = plugin;
        _gatherGroupCache  = new GatherGroupCache(Plugin.GatherGroupManager);
        _gatherWindowCache = new GatherWindowCache();
        _locationTable     = new LocationTable();
        _alarmCache        = new AlarmCache(Plugin.AlarmManager);
        _recordTable       = new RecordTable();
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(MinSize,     17 * ImGui.GetTextLineHeightWithSpacing() / ImGuiHelpers.GlobalScale),
            MaximumSize = new Vector2(MinSize * 4, ImGui.GetIO().DisplaySize.Y * 15 / 16 / ImGuiHelpers.GlobalScale),
        };
        IsOpen             = GatherBuddy.Config.OpenOnStart;
        UpdateFlags();
    }

    public override void Draw()
    {
        SetupValues();
        DrawHeader();
        using var tab = ImRaii.TabBar("ConfigTabs###GatherBuddyConfigTabs", ImGuiTabBarFlags.Reorderable);
        if (!tab)
            return;

        DrawItemTab();
        DrawFishTab();
        DrawWeatherTab();
        DrawAlarmTab();
        DrawGatherGroupTab();
        DrawGatherWindowTab();
        DrawLocationsTab();
        DrawRecordTab();
        DrawDebugTab();
        DrawConfigTab();
        DrawAbout();
    }

    public void UpdateFlags()
    {
        if (GatherBuddy.Config.MainWindowLockPosition)
            Flags |= ImGuiWindowFlags.NoMove;
        else
            Flags &= ~ImGuiWindowFlags.NoMove;

        if (GatherBuddy.Config.MainWindowLockResize)
            Flags |= ImGuiWindowFlags.NoResize;
        else
            Flags &= ~ImGuiWindowFlags.NoResize;
        RespectCloseHotkey = GatherBuddy.Config.CloseOnEscape;
    }

    public override void PreOpenCheck()
    {
        if (_earliestKeyboardToggle > GatherBuddy.Time.ServerTime || !Functions.CheckKeyState(GatherBuddy.Config.MainInterfaceHotkey, false))
            return;

        _earliestKeyboardToggle = GatherBuddy.Time.ServerTime.AddMilliseconds(500);
        Toggle();
    }

    public void Dispose()
    {
        _headerCache.Dispose();
        _weatherTable.Dispose();
        _itemTable.Dispose();
    }
}
