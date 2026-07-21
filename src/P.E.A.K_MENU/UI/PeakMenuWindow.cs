﻿using P.E.A.K_MENU.Constants;
using UnityEngine;

namespace P.E.A.K_MENU.UI;

internal sealed class PeakMenuWindow
{
    private readonly CursorController _cursorController = new();

    private bool _isOpen;
    private bool _positionInitialized;

    private Rect _windowRect;

    internal bool IsOpen => _isOpen;

    internal void Toggle()
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    internal void Open()
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        _cursorController.Release();

        Plugin.Log.LogInfo("P.E.A.K_MENU opened.");
    }

    internal void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        _cursorController.Restore();

        Plugin.Log.LogInfo("P.E.A.K_MENU closed.");
    }

    internal void Draw()
    {
        if (!_isOpen)
        {
            return;
        }

        InitializePosition();

        _windowRect = GUI.Window(
            ModConstants.WindowId,
            _windowRect,
            DrawWindowContents,
            ModConstants.WindowTitle
        );

        ClampToScreen();
    }

    private void InitializePosition()
    {
        if (_positionInitialized)
        {
            return;
        }

        _windowRect = new Rect(
            Screen.width - ModConstants.WindowWidth - ModConstants.WindowMargin,
            ModConstants.WindowTop,
            ModConstants.WindowWidth,
            ModConstants.WindowHeight
        );

        _positionInitialized = true;
    }

    private void DrawWindowContents(int windowId)
    {
        GUILayout.Space(10f);

        GUILayout.Label("P.E.A.K MENU");
        GUILayout.Label($"按 {ModConstants.ToggleMenuKey} 打开或关闭");

        GUILayout.Space(15f);

        if (GUILayout.Button("测试按钮", GUILayout.Height(35f)))
        {
            Plugin.Log.LogInfo("Test button clicked.");
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("关闭", GUILayout.Height(35f)))
        {
            Close();
        }

        GUI.DragWindow(
            new Rect(
                0f,
                0f,
                ModConstants.WindowWidth,
                30f
            )
        );
    }

    private void ClampToScreen()
    {
        _windowRect.x = Mathf.Clamp(
            _windowRect.x,
            0f,
            Mathf.Max(0f, Screen.width - _windowRect.width)
        );

        _windowRect.y = Mathf.Clamp(
            _windowRect.y,
            0f,
            Mathf.Max(0f, Screen.height - _windowRect.height)
        );
    }
}