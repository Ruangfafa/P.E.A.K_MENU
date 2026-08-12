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

        MaintainReleased();
    }

    internal void MaintainReleased()
    {
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (!Cursor.visible)
        {
            Cursor.visible = true;
        }
    }

    internal void Restore()
    {
        if (!_stateSaved)
        {
            return;
        }

        if (Cursor.lockState != _previousLockMode)
        {
            Cursor.lockState = _previousLockMode;
        }

        if (Cursor.visible != _previousVisible)
        {
            Cursor.visible = _previousVisible;
        }

        _stateSaved = false;
    }
}
