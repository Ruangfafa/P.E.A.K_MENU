using UnityEngine;

namespace P.E.A.K_MENU.UI;

internal sealed class CursorController
{
    private bool _previousVisible;
    private CursorLockMode _previousLockMode;
    private bool _stateSaved;

    internal void Release()
    {
        if (!_stateSaved)
        {
            _previousVisible = Cursor.visible;
            _previousLockMode = Cursor.lockState;
            _stateSaved = true;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    internal void Restore()
    {
        if (!_stateSaved)
        {
            return;
        }

        Cursor.visible = _previousVisible;
        Cursor.lockState = _previousLockMode;
        _stateSaved = false;
    }
}