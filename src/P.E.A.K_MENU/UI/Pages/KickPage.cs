using BepInEx.Configuration;
using P.E.A.K_MENU.Input;
using Photon.Pun;
using UnityEngine;
namespace P.E.A.K_MENU.UI.Pages;
internal sealed class KickPage : IMenuPage
{
    private static ConfigEntry<bool> _enabled = null!;
    private static ConfigEntry<float> _force = null!;
    private static Character? _target;
    private static float _nextKick;
    private static float _pressStarted = -1f;
    private static bool _longPressFired;
    private static Animator? _preparingAnimator;
    private static float _savedAnimationSpeed;
    private static int _savedAnimationState;
    private static float _savedAnimationTime;
    private static float _poseStarted;
    internal static bool Enabled => _enabled != null && _enabled.Value;
    private static bool _kickInProgress;
    private static TMPro.TextMeshProUGUI? _lockLabel;
    private readonly ShortcutRebindControl _rebind = new();
    public string Title => "踹人";
    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind("Kick", "Enabled", false, "启用踹人。");
        _force = config.Bind("Kick", "Force", 100f, new ConfigDescription("踹人力度：100–2000，每100一档；100对应原来的200力度。", new AcceptableValueRange<float>(100f, 2000f)));
        _force.Value = SnapForce(_force.Value);
        _enabled.SettingChanged += (_, _) =>
        {
            if (Enabled) return;
            CancelInput();
            foreach (var grabbing in Resources.FindObjectsOfTypeAll<CharacterGrabbing>())
                if (grabbing != null && grabbing.gameObject.scene.IsValid()) grabbing.StopAllCoroutines();
            foreach (var character in Resources.FindObjectsOfTypeAll<Character>())
                if (character != null && character.gameObject.scene.IsValid()) character.data.isKicking = false;
        };
    }
    public void Draw(MenuStyles styles)
    {
        _rebind.CaptureEvent();

        using (new GUILayout.HorizontalScope(GUILayout.Height(38f)))
        {
            _enabled.Value = GUILayout.Toggle(
                _enabled.Value, "启用踹人", styles.Toggle,
                GUILayout.MinWidth(110f), GUILayout.ExpandWidth(true),
                GUILayout.Height(38f));
            _rebind.DrawButtons(FeatureShortcutAction.ToggleKick, styles);
        }

        GUILayout.Space(12f);
        GUILayout.Label($"踹人力度：{_force.Value:0}", styles.Label);
        float nextForce = SnapForce(GUILayout.HorizontalSlider(_force.Value, 100f, 2000f));
        if (nextForce != _force.Value) _force.Value = nextForce;
        GUILayout.Space(12f);

        DrawShortcutRow("踹人（短按 / 长按）", FeatureShortcutAction.Kick, styles);
        GUILayout.Label("短按普通踹；长按抬脚准备并随视角选人，松开飞踹。中心范围内无人时松开取消。", styles.MutedLabel);
        GUILayout.Space(10f);
    }

    private void DrawShortcutRow(
        string label, FeatureShortcutAction action, MenuStyles styles)
    {
        using (new GUILayout.HorizontalScope(GUILayout.Height(38f)))
        {
            GUILayout.Label(label, styles.Label,
                GUILayout.MinWidth(110f), GUILayout.ExpandWidth(true),
                GUILayout.Height(38f));
            _rebind.DrawButtons(action, styles);
        }
    }

    private static float SnapForce(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return 100f;
        return Mathf.Round(Mathf.Clamp(value, 100f, 2000f) / 100f) * 100f;
    }

    private static System.Collections.IEnumerator KickAfterWarp(Character local, CharacterGrabbing grabbing, Character target, bool warped)
    {
        _kickInProgress = true;
        float oldForce = grabbing.kickForce;
        float oldDelay = grabbing.kickDelay;
        var ignored = new System.Collections.Generic.List<(Collider, Collider)>();
        try
        {
            if (warped)
            {
                foreach (var own in local.GetComponentsInChildren<Collider>())
                foreach (var other in target.GetComponentsInChildren<Collider>())
                    if (!own.isTrigger && !other.isTrigger && !Physics.GetIgnoreCollision(own, other))
                    { Physics.IgnoreCollision(own, other, true); ignored.Add((own, other)); }
                Vector3 direction = target.Center - local.Center;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.001f) direction = local.transform.forward;
                direction.Normalize();
                Vector3 destination = target.Center - direction * 0.9f;
                HarmonyLib.AccessTools.Method(typeof(Character), "WarpPlayer", new[] { typeof(Vector3), typeof(bool) })
                    .Invoke(local, new object[] { destination, false });
                float deadline = Time.unscaledTime + 3f;
                yield return null;
                while (local != null && Vector3.Distance(local.Center, destination) > 0.2f && Time.unscaledTime < deadline) yield return null;
                if (local == null || target == null || Vector3.Distance(local.Center, destination) > 0.2f) yield break;
                local.data.lookDirection = direction;
                foreach (var body in local.GetComponentsInChildren<Rigidbody>())
                    if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            }
            if (local == null || grabbing == null || !_enabled.Value) yield break;
            grabbing.kickForce = SnapForce(_force.Value) * 2f;
            if (warped)
            {
                ApplyInstantFlyingKick(local, grabbing, target);
                // Collision protection only: the hit has already happened above.
                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                local.photonView.RPC("RPCA_Kick", RpcTarget.All);
                yield return new WaitForSeconds(grabbing.kickDelay + 0.25f);
            }
        }
        finally
        {
            foreach (var pair in ignored)
                if (pair.Item1 != null && pair.Item2 != null) Physics.IgnoreCollision(pair.Item1, pair.Item2, false);
            if (grabbing != null) { grabbing.kickForce = oldForce; grabbing.kickDelay = oldDelay; }
            if (local != null) local.data.isKicking = false;
            _kickInProgress = false;
        }
    }
    private static void ApplyInstantFlyingKick(Character local, CharacterGrabbing grabbing, Character target)
    {
        if (!Enabled || target == null || !target.gameObject.activeInHierarchy || !target.IsRegisteredToPlayer) return;
        Vector3 direction = target.Center - local.Center;
        if (direction.sqrMagnitude < 0.001f) direction = local.data.lookDirection;
        if (direction.y > 0f) direction.y = 0f;
        direction.Normalize();
        float stamina = local.data.currentStamina;
        local.UseStamina(1f);
        local.data.isKicking = true;
        local.refs.animator.SetTrigger("Kick");
        // Other clients play the native animation, but only the owner runs KickCast.
        // Do not call RPCA_Kick locally: its coroutine would deliver a second hit.
        local.photonView.RPC("RPCA_Kick", RpcTarget.Others);
        Vector3 impact = target.Center;
        local.photonView.RPC("RPCA_KickImpact", RpcTarget.All, impact);
        if (target.IsStuck() && target.IsLocal) target.UnStick();
        target.Fall(grabbing.kickRagdollTime);
        target.AddForceAtPosition(direction * grabbing.kickForce * (1f + (stamina - 0.1f) * 2f), impact, grabbing.kickRange);
        target.refs.afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Web, 1f);
        target.refs.afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.FlyTrap, 1f);
        if (grabbing.kickParticle != null) grabbing.kickParticle.Play();
    }

    internal static void HideLockName()
    {
        CancelInput();
        _target = null;
        if (_lockLabel != null) UnityEngine.Object.Destroy(_lockLabel.transform.parent.gameObject);
        _lockLabel = null;
    }

    internal static void DrawAim()
    {
        if (_lockLabel != null) _lockLabel.enabled = false;
        if (_enabled == null || !_enabled.Value || MenuState.IsOpen || Character.localCharacter == null) return;
        var binding = FeatureInputSettings.GetBinding(FeatureShortcutAction.Kick);
        if (!System.Enum.TryParse<KeyCode>(binding.Value, true, out var key) || key == KeyCode.None || !UnityEngine.Input.GetKey(key)) _target = null;
        if (_target == null) return;
        if (_lockLabel == null)
        {
            foreach (var slot in Resources.FindObjectsOfTypeAll<PlayerName>())
            {
                if (slot == null || !slot.gameObject.scene.IsValid() || slot.text == null || slot.text.font == null) continue;
                var root = new GameObject("PeakMenuLockName", typeof(Canvas));
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = slot.text.canvas != null ? slot.text.canvas.rootCanvas.sortingOrder : 0;
                var label = new GameObject("LockName", typeof(RectTransform));
                label.transform.SetParent(root.transform, false);
                _lockLabel = label.AddComponent<TMPro.TextMeshProUGUI>();
                _lockLabel.font = slot.text.font;
                _lockLabel.fontSharedMaterial = slot.text.fontSharedMaterial;
                _lockLabel.fontSize = 24f;
                _lockLabel.color = new Color32(255, 209, 102, 255);
                _lockLabel.alignment = TMPro.TextAlignmentOptions.Center;
                _lockLabel.richText = false;
                _lockLabel.raycastTarget = false;
                _lockLabel.rectTransform.anchorMin = _lockLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _lockLabel.rectTransform.anchoredPosition = new Vector2(0f, -36f);
                _lockLabel.rectTransform.sizeDelta = new Vector2(480f, 60f);
                break;
            }
        }
        if (_lockLabel != null) { _lockLabel.text = _target.characterName; _lockLabel.enabled = true; }
    }
    private static Character? Aim()
    {
        var camera = Camera.main;
        if (camera == null) return null;
        Character? closest = null;
        float bestAngle = 12f;
        foreach (var target in Character.AllCharacters)
        {
            if (target == null || target.IsLocal || !target.gameObject.activeInHierarchy || !target.IsPlayerControlled || !target.IsRegisteredToPlayer) continue;
            Vector3 offset = target.Center - camera.transform.position;
            if (offset.sqrMagnitude < 0.001f) continue;
            float angle = Vector3.Angle(camera.transform.forward, offset);
            if (angle >= bestAngle) continue;
            bestAngle = angle;
            closest = target;
        }
        return closest;
    }
    private static void BeginPreparation()
    {
        var local = Character.localCharacter;
        if (local == null || _kickInProgress || local.warping) return;
        _preparingAnimator = local.refs.animator;
        if (_preparingAnimator == null) return;
        _savedAnimationSpeed = _preparingAnimator.speed;
        var state = _preparingAnimator.GetCurrentAnimatorStateInfo(0);
        _savedAnimationState = state.fullPathHash;
        _savedAnimationTime = state.normalizedTime;
        _poseStarted = Time.unscaledTime;
        // Visual preparation only: never invoke KickCast/RPCA_Kick here.
        _preparingAnimator.SetTrigger("Kick");
    }
    private static void EndPreparation()
    {
        if (_preparingAnimator != null)
        {
            _preparingAnimator.speed = _savedAnimationSpeed;
            _preparingAnimator.ResetTrigger("Kick");
            _preparingAnimator.Play(_savedAnimationState, 0, _savedAnimationTime);
        }
        _preparingAnimator = null;
    }
    internal static void CancelInput()
    {
        EndPreparation();
        _pressStarted = -1f;
        _longPressFired = false;
        _target = null;
        if (_lockLabel != null) _lockLabel.enabled = false;
    }
    internal static void UpdateInput()
    {
        if (FeatureInputSettings.IsPressed(FeatureShortcutAction.ToggleKick)) _enabled.Value = !_enabled.Value;
        if (!Enabled || Character.localCharacter == null) { CancelInput(); return; }
        var binding = FeatureInputSettings.GetBinding(FeatureShortcutAction.Kick);
        if (!System.Enum.TryParse<KeyCode>(binding.Value, true, out var key) || key == KeyCode.None)
        {
            CancelInput();
            if (binding.IsPressed()) PerformKick(false);
            return;
        }
        if (UnityEngine.Input.GetKeyDown(key)) { _pressStarted = Time.unscaledTime; _longPressFired = false; }
        if (_pressStarted < 0f) return;
        if (UnityEngine.Input.GetKey(key))
        {
            if (!_longPressFired && Time.unscaledTime - _pressStarted >= 0.45f)
            {
                _longPressFired = true;
                BeginPreparation();
            }
            if (_longPressFired)
            {
                _target = Aim();
                if (_preparingAnimator != null && Time.unscaledTime - _poseStarted >= 0.15f)
                    _preparingAnimator.speed = 0f;
            }
            return;
        }
        if (_longPressFired)
        {
            // Re-evaluate on release, so looking away always cancels the attack.
            _target = Aim();
            EndPreparation();
            if (_target != null) PerformKick(true);
        }
        else PerformKick(false);
        CancelInput();
    }
    private static void PerformKick(bool flying)
    {
        var local = Character.localCharacter;
        if (!Enabled || local == null) return;
        var target = flying ? _target : null;
        if (flying && (target == null || !target.gameObject.activeInHierarchy)) return;
        var grabbing = local.GetComponent<CharacterGrabbing>();
        if (grabbing == null || _kickInProgress || local.warping || Time.time < _nextKick) return;
        _nextKick = Time.time + 1.2f;
        local.StartCoroutine(KickAfterWarp(local, grabbing, target!, flying));
    }
}
