using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using P.E.A.K_MENU.Features.Status;
using P.E.A.K_MENU.Input;
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
        16f;

    private const float MaximumFlightSpeed =
        255f;

    internal const float DefaultHoverDownForce =
        255f;

    private const float MinimumHoverDownForce =
        0f;

    private const float MaximumHoverDownForce =
        500f;

    /*
     * 两次空格按下之间的最大间隔。
     */
    private const float DoubleTapInterval =
        0.30f;

    /*
     * 滚轮每格增加或减少的速度。
     */
    private const float ScrollSpeedStep =
        16f;

    /*
     * 退出飞行后提供 2 秒缓降。
     */
    private const float SlowFallDuration =
        2f;

    /*
     * 缓降阶段允许的最大向下速度。
     */
    private const float MaximumSlowFallSpeed =
        -6f;

    private bool _enabled;
    private bool _activelyFlying;

    private readonly ConfigEntry<float>
        _hoverDownForce;

    private readonly ConfigEntry<bool>
        _horizontalWasdMovement;

    private float _flightSpeed =
        DefaultFlightSpeed;

    private float _lastSpacePressTime =
        -100f;

    private Character? _activeCharacter;

    private bool _savedInvincible;
    private bool _savedAntiKnockback;
    private bool _hasSavedStatusState;

    private float _slowFallUntil;

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

    internal float HoverDownForce =>
        Mathf.Clamp(
            _hoverDownForce.Value,
            MinimumHoverDownForce,
            MaximumHoverDownForce
        );

    internal bool HorizontalWasdMovement =>
        _horizontalWasdMovement.Value;

    internal FlightService(
        ConfigFile config)
    {
        _hoverDownForce = config.Bind(
            "Flight",
            "HoverDownForce",
            DefaultHoverDownForce,
            new ConfigDescription(
                "实际飞行浮空时的向下重力补偿力。" +
                "增加可抑制自动上浮，减少可缓解自动下沉。",
                new AcceptableValueRange<float>(
                    MinimumHoverDownForce,
                    MaximumHoverDownForce
                )
            )
        );

        _horizontalWasdMovement = config.Bind(
            "Flight",
            "HorizontalWasdMovement",
            false,
            "WASD 是否仅进行水平平移。关闭时 W/S 会跟随视角俯仰。"
        );
    }

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

        if (_activelyFlying)
        {
            MaintainStatusProtection();
        }

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

        if (FeatureInputSettings
            .DoubleTapFlightEnabled)
        {
            DetectDoubleSpace();
        }
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

    internal void AdjustFlightSpeed(
        float direction)
    {
        if (!_enabled ||
            !_activelyFlying ||
            Mathf.Approximately(direction, 0f))
        {
            return;
        }

        float previousSpeed =
            _flightSpeed;

        _flightSpeed = Mathf.Clamp(
            _flightSpeed +
            Mathf.Sign(direction) *
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

        LastSucceeded = true;
        LastStatus =
            direction > 0f
                ? $"提高飞行速度：{_flightSpeed:0.##}。"
                : $"降低飞行速度：{_flightSpeed:0.##}。";
    }

    internal void AdjustHoverDownForce(
        float direction)
    {
        if (!_enabled ||
            !_activelyFlying ||
            Mathf.Approximately(direction, 0f))
        {
            return;
        }

        _hoverDownForce.Value = Mathf.Clamp(
            HoverDownForce + direction,
            MinimumHoverDownForce,
            MaximumHoverDownForce
        );

        LastSucceeded = true;
        LastStatus =
            $"浮空重力校准已调整为 " +
            $"{HoverDownForce:0.##}。";
    }

    internal void SetHorizontalWasdMovement(
        bool horizontal)
    {
        if (_horizontalWasdMovement.Value ==
            horizontal)
        {
            return;
        }

        _horizontalWasdMovement.Value =
            horizontal;

        LastSucceeded = true;
        LastStatus = horizontal
            ? "WASD 已切换为水平平移。"
            : "WASD 已切换为随视角飞行。";
    }

    internal void ResetHoverDownForce()
    {
        _hoverDownForce.Value =
            DefaultHoverDownForce;

        LastSucceeded = true;
        LastStatus =
            $"浮空重力校准已恢复默认值 " +
            $"{DefaultHoverDownForce:0.##}。";
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

        if (restoreStatusState)
        {
            RestoreStatusProtection();
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
                ? "飞行已关闭，已恢复此前状态，并获得 2 秒缓降保护。"
                : "飞行总开关已关闭。";

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

            if (!BeginStatusProtection())
            {
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

        RestoreStatusProtection();

        /*
         * 每次退出实际飞行，
         * 固定给予 2 秒缓降。
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
            "已退出飞行：获得 2 秒缓降保护。";

        Plugin.Log.LogInfo(
            "Physical flight disabled."
        );
    }

    private bool BeginStatusProtection()
    {
        if (!StatusRuntime.IsInitialized)
        {
            LastSucceeded =
                false;

            LastStatus =
                "状态功能尚未初始化，无法进入飞行状态。";

            return false;
        }

        StatusService statusService =
            StatusRuntime.Service;

        /*
         * 每次进入实际飞行时保存当前保护状态，
         * 退出实际飞行后原样恢复。
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
         * 飞行控制器直接向布娃娃刚体施加飞行力，
         * 不经过防击退补丁拦截的 Character 外力方法，
         * 因此可以在飞行期间同时保持防击退。
         */
        statusService.SetAntiKnockback(
            true,
            force: true
        );

        return true;
    }

    private void RestoreStatusProtection()
    {
        if (!_hasSavedStatusState)
        {
            return;
        }

        if (StatusRuntime.IsInitialized)
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

        _hasSavedStatusState =
            false;
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

            if (!_activelyFlying)
            {
                _flightSpeed =
                    DefaultFlightSpeed;
            }

            ToggleActiveFlight();

            return;
        }

        _lastSpacePressTime =
            currentTime;
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

            /*
             * 保留原始重力，仅限制退出飞行瞬间的
             * 最大向下速度。
             */
            Vector3 velocity =
                rigidbody.linearVelocity;

            if (velocity.y <
                MaximumSlowFallSpeed)
            {
                velocity.y =
                    MaximumSlowFallSpeed;

                rigidbody.linearVelocity =
                    velocity;
            }
        }

        _slowFallWasApplied =
            _slowFallGravityStates.Count >
            0;

        if (!_slowFallWasApplied)
        {
            _slowFallUntil =
                0f;

            return;
        }

        Plugin.Log.LogInfo(
            $"Flight exit protection applied: " +
            $"{SlowFallDuration:0.##} seconds " +
            $"slow fall."
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
         * 2 秒结束后恢复正常下落。
         */
        if (currentTime >=
            _slowFallUntil)
        {
            ClearSlowFallImmediately();
            return;
        }

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

            /*
             * 全程保留原始重力，
             * 仅限制最大下落速度。
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

        _slowFallUntil =
            0f;

        _slowFallWasApplied =
            false;
    }

    private void ClearSlowFallImmediately()
    {
        RestoreSlowFallGravityStates();

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
         * 飞行力直接施加到布娃娃刚体，
         * 可以与 Character 外力层的防击退同时生效。
         */
        if (!service.AntiKnockback)
        {
            service.SetAntiKnockback(
                true,
                force: true
            );
        }
    }
}
