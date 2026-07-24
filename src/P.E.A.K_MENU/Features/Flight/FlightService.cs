using System;
using System.Collections.Generic;
using System.Linq;
using P.E.A.K_MENU.Features.Status;
using P.E.A.K_MENU.UI;
using UnityEngine;

namespace P.E.A.K_MENU.Features.Flight;

/// <summary>
/// 本地玩家物理式飞行服务。
///
/// 实际施力由 FlightController 负责；
/// 本服务负责开关、双击检测、滚轮调速、
/// 状态保护和退出飞行后的缓降。
/// </summary>
internal sealed class FlightService :
    IDisposable
{
    private const float DefaultFlightSpeed =
        16f;

    private const float MinimumFlightSpeed =
        0.5f;

    private const float MaximumFlightSpeed =
        100f;

    /*
     * 两次空格按下之间的最大间隔。
     */
    private const float DoubleTapInterval =
        0.30f;

    /*
     * 滚轮每格增加或减少的速度。
     */
    private const float ScrollSpeedStep =
        5f;

    /*
     * 退出飞行后的保护：
     *
     * 前 1 秒完全无重力；
     * 总计 5 秒缓降。
     */
    private const float ZeroGravityDuration =
        1f;

    private const float SlowFallDuration =
        5f;

    /*
     * 缓降阶段允许的最大向下速度。
     */
    private const float MaximumSlowFallSpeed =
        -2.25f;

    private bool _enabled;
    private bool _activelyFlying;

    private float _flightSpeed =
        DefaultFlightSpeed;

    private float _lastSpacePressTime =
        -100f;

    private Character? _activeCharacter;

    private bool _savedInvincible;
    private bool _savedAntiKnockback;
    private bool _hasSavedStatusState;

    private float _slowFallUntil;
    private float _zeroGravityUntil;

    private bool _slowFallWasApplied;

    /*
     * 保存退出飞行缓降开始前，
     * 每个刚体原本的重力状态。
     */
    private readonly Dictionary<Rigidbody, bool>
        _slowFallGravityStates =
            new();

    internal bool Enabled =>
        _enabled;

    internal bool ActivelyFlying =>
        _activelyFlying;

    internal float FlightSpeed =>
        _flightSpeed;

    internal string LastStatus
    {
        get;
        private set;
    } = "飞行功能尚未开启。";

    internal bool LastSucceeded
    {
        get;
        private set;
    }

    internal void Update()
    {
        /*
         * 缓降可以在飞行总开关关闭后继续生效，
         * 因此必须始终维护。
         */
        MaintainSlowFall();

        if (!_enabled)
        {
            return;
        }

        MaintainStatusProtection();

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            _activeCharacter =
                null;

            LastSucceeded =
                false;

            LastStatus =
                "未找到本地玩家，请先进入关卡。";

            return;
        }

        if (!ReferenceEquals(
                _activeCharacter,
                character))
        {
            _activeCharacter =
                character;

            EnsureFlightController(
                character
            );

            Plugin.Log.LogInfo(
                $"Flight character changed: " +
                $"{character.name}."
            );
        }

        /*
         * 菜单打开时不检测双击空格，
         * 也不读取滚轮调速。
         */
        if (MenuState.IsOpen)
        {
            return;
        }

        /*
         * 滚轮调速固定启用。
         */
        if (_activelyFlying)
        {
            ReadMouseWheelSpeed();
        }

        /*
         * 双击空格固定启用。
         */
        DetectDoubleSpace();
    }

    internal void SetEnabled(
        bool enabled)
    {
        if (_enabled ==
            enabled)
        {
            return;
        }

        if (enabled)
        {
            EnableFlightSystem();
            return;
        }

        DisableFlightSystem(
            restoreStatusState: true
        );
    }

    internal void SetFlightSpeed(
        float speed)
    {
        if (float.IsNaN(
                speed) ||
            float.IsInfinity(
                speed))
        {
            LastSucceeded =
                false;

            LastStatus =
                "飞行速度格式无效。";

            return;
        }

        _flightSpeed =
            Mathf.Clamp(
                speed,
                MinimumFlightSpeed,
                MaximumFlightSpeed
            );

        LastSucceeded =
            true;

        LastStatus =
            $"飞行速度已设为 " +
            $"{_flightSpeed:0.##}。";
    }

    internal void ToggleActiveFlight()
    {
        if (!_enabled)
        {
            LastSucceeded =
                false;

            LastStatus =
                "请先开启飞行总开关。";

            return;
        }

        SetActiveFlight(
            !_activelyFlying
        );
    }

    public void Dispose()
    {
        DisableFlightSystem(
            restoreStatusState: true
        );

        ClearSlowFallImmediately();

        _activeCharacter =
            null;
    }

    private void EnableFlightSystem()
    {
        if (!StatusRuntime.IsInitialized)
        {
            LastSucceeded =
                false;

            LastStatus =
                "状态功能尚未初始化，无法开启飞行保护。";

            return;
        }

        Character? character =
            Character.localCharacter;

        if (character is null ||
            !character.IsLocal)
        {
            LastSucceeded =
                false;

            LastStatus =
                "未找到本地玩家，请先进入关卡。";

            return;
        }

        StatusService statusService =
            StatusRuntime.Service;

        /*
         * 保存进入飞行系统前的状态。
         */
        _savedInvincible =
            statusService.Invincible;

        _savedAntiKnockback =
            statusService.AntiKnockback;

        _hasSavedStatusState =
            true;

        statusService.SetFlightProtectionLock(
            true
        );

        statusService.SetInvincible(
            true,
            force: true
        );

        /*
         * 物理式飞行会对角色施加力。
         *
         * 飞行系统启用期间关闭防击退，
         * 避免防击退补丁抵消飞行作用力。
         */
        statusService.SetAntiKnockback(
            false,
            force: true
        );

        EnsureFlightController(
            character
        );

        _enabled =
            true;

        _activelyFlying =
            false;

        _activeCharacter =
            character;

        _lastSpacePressTime =
            -100f;

        LastSucceeded =
            true;

        LastStatus =
            "飞行总开关已开启。双击空格开始飞行。";

        Plugin.Log.LogInfo(
            "Flight system enabled in standby mode."
        );
    }

    private void DisableFlightSystem(
        bool restoreStatusState)
    {
        Character? character =
            Character.localCharacter;

        bool wasActivelyFlying =
            _activelyFlying;

        _activelyFlying =
            false;

        _enabled =
            false;

        _activeCharacter =
            null;

        _lastSpacePressTime =
            -100f;

        /*
         * 关闭总开关时如果仍在飞行，
         * 固定给予退出飞行缓降。
         */
        if (wasActivelyFlying &&
            character is not null &&
            character.IsLocal)
        {
            RefreshSlowFall(
                character
            );
        }

        if (restoreStatusState &&
            _hasSavedStatusState &&
            StatusRuntime.IsInitialized)
        {
            StatusService statusService =
                StatusRuntime.Service;

            statusService.SetFlightProtectionLock(
                false
            );

            statusService.SetInvincible(
                _savedInvincible,
                force: true
            );

            statusService.SetAntiKnockback(
                _savedAntiKnockback,
                force: true
            );
        }
        else if (StatusRuntime.IsInitialized)
        {
            StatusRuntime
                .Service
                .SetFlightProtectionLock(
                    false
                );
        }

        _hasSavedStatusState =
            false;

        LastSucceeded =
            true;

        LastStatus =
            wasActivelyFlying
                ? "飞行已关闭，已恢复此前状态，并获得 1 秒无重力和总计 5 秒缓降保护。"
                : "飞行已关闭，并已恢复此前的无敌与防击退状态。";

        Plugin.Log.LogInfo(
            "Physical flight system disabled."
        );
    }

    private void SetActiveFlight(
        bool active)
    {
        if (!_enabled)
        {
            return;
        }

        if (_activelyFlying ==
            active)
        {
            return;
        }

        Character? character =
            Character.localCharacter;

        if (active)
        {
            if (character is null ||
                !character.IsLocal)
            {
                LastSucceeded =
                    false;

                LastStatus =
                    "未找到本地玩家，无法进入飞行状态。";

                return;
            }

            /*
             * 重新进入飞行时，
             * 先结束上一次退出缓降。
             */
            ClearSlowFallImmediately();

            _activeCharacter =
                character;

            EnsureFlightController(
                character
            );

            _activelyFlying =
                true;

            TryResetFalling(
                character
            );

            LastSucceeded =
                true;

            LastStatus =
                "已进入物理飞行状态。";

            Plugin.Log.LogInfo(
                "Physical flight enabled."
            );

            return;
        }

        _activelyFlying =
            false;

        /*
         * 每次退出实际飞行，
         * 固定给予 1 秒无重力和总计 5 秒缓降。
         */
        if (character is not null &&
            character.IsLocal)
        {
            RefreshSlowFall(
                character
            );
        }

        LastSucceeded =
            true;

        LastStatus =
            "已退出飞行：获得 1 秒无重力和总计 5 秒缓降保护。";

        Plugin.Log.LogInfo(
            "Physical flight disabled."
        );
    }

    private static void EnsureFlightController(
        Character character)
    {
        FlightController? controller =
            character.GetComponent<
                FlightController>();

        if (controller is not null)
        {
            return;
        }

        character.gameObject
            .AddComponent<
                FlightController>();

        Plugin.Log.LogInfo(
            $"FlightController added to " +
            $"{character.name} by FlightService."
        );
    }

    private void DetectDoubleSpace()
    {
        if (!UnityEngine.Input.GetKeyDown(
                KeyCode.Space))
        {
            return;
        }

        float currentTime =
            Time.unscaledTime;

        float elapsed =
            currentTime -
            _lastSpacePressTime;

        if (elapsed <=
            DoubleTapInterval)
        {
            _lastSpacePressTime =
                -100f;

            ToggleActiveFlight();

            return;
        }

        _lastSpacePressTime =
            currentTime;
    }

    private void ReadMouseWheelSpeed()
    {
        float scroll =
            UnityEngine.Input
                .mouseScrollDelta
                .y;

        if (Mathf.Abs(
                scroll) <
            0.01f)
        {
            return;
        }

        float direction =
            Mathf.Sign(
                scroll
            );

        float previousSpeed =
            _flightSpeed;

        _flightSpeed =
            Mathf.Clamp(
                _flightSpeed +
                direction *
                ScrollSpeedStep,
                MinimumFlightSpeed,
                MaximumFlightSpeed
            );

        if (Mathf.Approximately(
                previousSpeed,
                _flightSpeed))
        {
            return;
        }

        LastSucceeded =
            true;

        LastStatus =
            direction > 0f
                ? $"滚轮提高速度：{_flightSpeed:0.##}。"
                : $"滚轮降低速度：{_flightSpeed:0.##}。";
    }

    private void RefreshSlowFall(
        Character character)
    {
        /*
         * 恢复上一次缓降保存的状态，
         * 避免重复进入后保存错误重力。
         */
        RestoreSlowFallGravityStates();

        float currentTime =
            Time.unscaledTime;

        _zeroGravityUntil =
            currentTime +
            ZeroGravityDuration;

        _slowFallUntil =
            currentTime +
            SlowFallDuration;

        Rigidbody[] rigidbodies =
            character.GetComponentsInChildren<
                Rigidbody>(
                    true
                );

        foreach (Rigidbody rigidbody
                 in rigidbodies)
        {
            if (!IsUsableRigidbody(
                    rigidbody))
            {
                continue;
            }

            _slowFallGravityStates[
                rigidbody
            ] =
                rigidbody.useGravity;

            rigidbody.useGravity =
                false;

            /*
             * 清除退出飞行瞬间的向下速度。
             */
            Vector3 velocity =
                rigidbody.linearVelocity;

            if (velocity.y <
                0f)
            {
                velocity.y =
                    0f;

                rigidbody.linearVelocity =
                    velocity;
            }
        }

        _slowFallWasApplied =
            _slowFallGravityStates.Count >
            0;

        if (!_slowFallWasApplied)
        {
            _zeroGravityUntil =
                0f;

            _slowFallUntil =
                0f;

            return;
        }

        Plugin.Log.LogInfo(
            $"Flight exit protection applied: " +
            $"{ZeroGravityDuration:0.##} seconds " +
            $"zero gravity and " +
            $"{SlowFallDuration:0.##} seconds " +
            $"total slow fall."
        );
    }

    private void MaintainSlowFall()
    {
        if (!_slowFallWasApplied)
        {
            return;
        }

        float currentTime =
            Time.unscaledTime;

        /*
         * 总计 5 秒结束后恢复原始重力。
         */
        if (currentTime >=
            _slowFallUntil)
        {
            ClearSlowFallImmediately();
            return;
        }

        bool zeroGravityActive =
            currentTime <
            _zeroGravityUntil;

        foreach (
            KeyValuePair<Rigidbody, bool>
                entry
            in _slowFallGravityStates
                .ToArray())
        {
            Rigidbody rigidbody =
                entry.Key;

            if (!IsUsableRigidbody(
                    rigidbody))
            {
                _slowFallGravityStates.Remove(
                    rigidbody
                );

                continue;
            }

            if (zeroGravityActive)
            {
                /*
                 * 前 1 秒完全关闭重力，
                 * 并阻止继续产生向下速度。
                 */
                rigidbody.useGravity =
                    false;

                Vector3 velocity =
                    rigidbody.linearVelocity;

                if (velocity.y <
                    0f)
                {
                    velocity.y =
                        0f;

                    rigidbody.linearVelocity =
                        velocity;
                }

                continue;
            }

            /*
             * 第 1～5 秒恢复原始重力，
             * 但限制最大下落速度。
             */
            rigidbody.useGravity =
                entry.Value;

            Vector3 slowFallVelocity =
                rigidbody.linearVelocity;

            if (slowFallVelocity.y <
                MaximumSlowFallSpeed)
            {
                slowFallVelocity.y =
                    MaximumSlowFallSpeed;

                rigidbody.linearVelocity =
                    slowFallVelocity;
            }
        }

        if (_slowFallGravityStates.Count >
            0)
        {
            return;
        }

        _zeroGravityUntil =
            0f;

        _slowFallUntil =
            0f;

        _slowFallWasApplied =
            false;
    }

    private void ClearSlowFallImmediately()
    {
        RestoreSlowFallGravityStates();

        _zeroGravityUntil =
            0f;

        _slowFallUntil =
            0f;

        _slowFallWasApplied =
            false;
    }

    private void RestoreSlowFallGravityStates()
    {
        foreach (
            KeyValuePair<Rigidbody, bool>
                entry
            in _slowFallGravityStates
                .ToArray())
        {
            Rigidbody rigidbody =
                entry.Key;

            if (rigidbody is null)
            {
                continue;
            }

            try
            {
                rigidbody.useGravity =
                    entry.Value;
            }
            catch (Exception exception)
            {
                Plugin.Log.LogDebug(
                    $"Failed to restore slow-fall " +
                    $"gravity: {exception.Message}"
                );
            }
        }

        _slowFallGravityStates.Clear();
    }

    private static bool IsUsableRigidbody(
        Rigidbody? rigidbody)
    {
        if (rigidbody is null)
        {
            return false;
        }

        GameObject gameObject =
            rigidbody.gameObject;

        return
            gameObject is not null &&
            gameObject.scene.IsValid() &&
            gameObject.activeInHierarchy;
    }

    private static void TryResetFalling(
        Character character)
    {
        try
        {
            character.RPCA_UnFall();
        }
        catch (Exception exception)
        {
            Plugin.Log.LogDebug(
                $"RPCA_UnFall failed while " +
                $"starting flight: " +
                $"{exception.Message}"
            );
        }
    }

    private void MaintainStatusProtection()
    {
        if (!StatusRuntime.IsInitialized)
        {
            return;
        }

        StatusService service =
            StatusRuntime.Service;

        if (!service.FlightProtectionLock)
        {
            service.SetFlightProtectionLock(
                true
            );
        }

        if (!service.Invincible)
        {
            service.SetInvincible(
                true,
                force: true
            );
        }

        /*
         * 物理式飞行期间保持防击退关闭，
         * 避免防击退补丁移除飞行作用力。
         */
        if (service.AntiKnockback)
        {
            service.SetAntiKnockback(
                false,
                force: true
            );
        }
    }
}