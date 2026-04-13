using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerTargetingSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private Transform targetingOrigin;
    [SerializeField] private PlayerCombatController playerCombatController;

    [Header("Targeting State")]
    [SerializeField] private bool autoTargetingEnabled = true;
    [SerializeField] private CombatTeam targetTeam = CombatTeam.Enemy;

    [Header("Search")]
    [SerializeField] private LayerMask targetLayerMask = ~0;
    [SerializeField][Min(0.1f)] private float searchRadius = 4f;
    [SerializeField][Min(0.01f)] private float targetRefreshInterval = 0.10f;
    [SerializeField][Min(1f)] private float currentTargetRetentionMultiplier = 1.20f;
    [SerializeField][Range(0.10f, 1f)] private float currentTargetPriorityMultiplier = 0.85f;

    [Header("Attack Direction")]
    [SerializeField][Min(0f)] private float minHorizontalOffsetForSideDecision = 0.10f;

    [Header("Marker")]
    [SerializeField] private TargetMarkerView markerPrefab;
    [SerializeField] private Transform markerParent;
    [SerializeField] private bool createMarkerIfMissing = true;

    private readonly Collider2D[] targetSearchResults = new Collider2D[64];
    private readonly HashSet<int> seenTargetIds = new HashSet<int>();

    private CombatTarget currentTarget;
    private TargetMarkerView markerInstance;
    private float refreshTimer;

    public event Action<bool> OnAutoTargetingEnabledChanged;
    public event Action<CombatTarget> OnCurrentTargetChanged;

    public bool IsAutoTargetingEnabled => autoTargetingEnabled;
    public CombatTarget CurrentTarget => currentTarget;
    public bool HasValidTarget => IsTargetValid(currentTarget, searchRadius * currentTargetRetentionMultiplier);

    private void Awake()
    {
        ResolveReferences();
        EnsureMarkerInstance();
        UpdateMarkerVisual();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (gameInput != null)
            gameInput.OnToggleTargeting += HandleToggleTargeting;

        UpdateMarkerVisual();
    }

    private void OnDisable()
    {
        if (gameInput != null)
            gameInput.OnToggleTargeting -= HandleToggleTargeting;

        if (markerInstance != null)
        {
            markerInstance.SetTarget(null);
            markerInstance.SetVisible(false);
        }
    }

    private void Update()
    {
        if (!autoTargetingEnabled)
        {
            if (currentTarget != null)
                SetCurrentTarget(null);

            UpdateMarkerVisual();
            return;
        }

        if (currentTarget != null && !IsTargetValid(currentTarget, searchRadius * currentTargetRetentionMultiplier))
            SetCurrentTarget(null);

        refreshTimer += Time.unscaledDeltaTime;

        if (refreshTimer >= targetRefreshInterval)
        {
            refreshTimer = 0f;
            RefreshTarget();
        }

        UpdateMarkerVisual();
    }

    public void SetAutoTargetingEnabled(bool value)
    {
        if (autoTargetingEnabled == value)
            return;

        autoTargetingEnabled = value;
        refreshTimer = 0f;

        if (!autoTargetingEnabled)
        {
            SetCurrentTarget(null);
        }
        else
        {
            RefreshTarget();
        }

        UpdateMarkerVisual();
        OnAutoTargetingEnabledChanged?.Invoke(autoTargetingEnabled);
    }

    public void ForceRefreshTarget()
    {
        refreshTimer = 0f;
        RefreshTarget();
        UpdateMarkerVisual();
    }

    public bool TryGetPreferredAttackSide(PlayerAttackSide fallbackSide, out PlayerAttackSide side)
    {
        side = fallbackSide;

        if (!autoTargetingEnabled)
            return false;

        if (!IsTargetValid(currentTarget, searchRadius * currentTargetRetentionMultiplier))
            return false;

        Vector3 originPosition = GetOriginPosition();
        Vector3 delta = currentTarget.AimPoint.position - originPosition;

        if (Mathf.Abs(delta.x) <= minHorizontalOffsetForSideDecision)
        {
            side = fallbackSide;
            return true;
        }

        side = delta.x < 0f ? PlayerAttackSide.Left : PlayerAttackSide.Right;
        return true;
    }

    public bool TryGetCurrentTargetAimPoint(out Vector3 aimPoint)
    {
        if (IsTargetValid(currentTarget, searchRadius * currentTargetRetentionMultiplier))
        {
            aimPoint = currentTarget.AimPoint.position;
            return true;
        }

        aimPoint = Vector3.zero;
        return false;
    }

    private void HandleToggleTargeting()
    {
        SetAutoTargetingEnabled(!autoTargetingEnabled);
    }

    private void RefreshTarget()
    {
        if (!autoTargetingEnabled)
        {
            SetCurrentTarget(null);
            return;
        }

        Vector3 originPosition = GetOriginPosition();
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            originPosition,
            searchRadius,
            targetSearchResults,
            targetLayerMask
        );

        CombatTarget bestTarget = null;
        float bestScore = float.MaxValue;

        seenTargetIds.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = targetSearchResults[i];

            if (hitCollider == null)
                continue;

            CombatTarget candidate = GetCombatTargetFromCollider(hitCollider);

            if (!IsCandidateValid(candidate, searchRadius))
                continue;

            int candidateId = candidate.GetInstanceID();
            if (!seenTargetIds.Add(candidateId))
                continue;

            float distance = Vector2.Distance(originPosition, candidate.AimPoint.position);
            float score = distance;

            if (candidate == currentTarget)
                score *= currentTargetPriorityMultiplier;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        if (bestTarget != null)
        {
            SetCurrentTarget(bestTarget);
            return;
        }

        if (!IsTargetValid(currentTarget, searchRadius * currentTargetRetentionMultiplier))
            SetCurrentTarget(null);
    }

    private bool IsCandidateValid(CombatTarget candidate, float maxDistance)
    {
        if (candidate == null)
            return false;

        if (!candidate.IsTargetable)
            return false;

        if (candidate.Team != targetTeam)
            return false;

        float distance = Vector2.Distance(GetOriginPosition(), candidate.AimPoint.position);
        if (distance > maxDistance)
            return false;

        return true;
    }

    private bool IsTargetValid(CombatTarget target, float maxDistance)
    {
        if (target == null)
            return false;

        if (!target.IsTargetable)
            return false;

        if (target.Team != targetTeam)
            return false;

        float distance = Vector2.Distance(GetOriginPosition(), target.AimPoint.position);
        if (distance > maxDistance)
            return false;

        return true;
    }

    private CombatTarget GetCombatTargetFromCollider(Collider2D hitCollider)
    {
        if (hitCollider == null)
            return null;

        CombatTarget target = hitCollider.GetComponentInParent<CombatTarget>();

        if (target == null)
            target = hitCollider.GetComponent<CombatTarget>();

        return target;
    }

    private void SetCurrentTarget(CombatTarget newTarget)
    {
        if (currentTarget == newTarget)
            return;

        currentTarget = newTarget;
        OnCurrentTargetChanged?.Invoke(currentTarget);
    }

    private Vector3 GetOriginPosition()
    {
        return targetingOrigin != null ? targetingOrigin.position : transform.position;
    }

    private void ResolveReferences()
    {
        if (gameInput == null)
            gameInput = FindFirstObjectByType<GameInput>();

        if (playerCombatController == null)
            playerCombatController = GetComponent<PlayerCombatController>();

        if (targetingOrigin == null)
            targetingOrigin = transform;
    }

    private void EnsureMarkerInstance()
    {
        if (markerInstance != null)
            return;

        Transform parent = markerParent != null ? markerParent : transform;

        if (markerPrefab != null)
        {
            markerInstance = Instantiate(markerPrefab, parent);
            markerInstance.name = "TargetMarker_Instance";
            return;
        }

        if (!createMarkerIfMissing)
            return;

        markerInstance = CreateFallbackMarker(parent);
    }

    private TargetMarkerView CreateFallbackMarker(Transform parent)
    {
        GameObject markerObject = new GameObject("TargetMarker_Fallback");
        markerObject.transform.SetParent(parent, false);

        TextMeshPro textMesh = markerObject.AddComponent<TextMeshPro>();
        textMesh.text = "▼";
        textMesh.fontSize = 3.2f;
        textMesh.alignment = TextAlignmentOptions.Center;

        TargetMarkerView view = markerObject.AddComponent<TargetMarkerView>();
        return view;
    }

    private void UpdateMarkerVisual()
    {
        EnsureMarkerInstance();

        if (markerInstance == null)
            return;

        bool shouldShow =
            autoTargetingEnabled &&
            currentTarget != null &&
            currentTarget.IsTargetable;

        if (!shouldShow)
        {
            markerInstance.SetTarget(null);
            markerInstance.SetVisible(false);
            return;
        }

        markerInstance.SetTarget(currentTarget.AimPoint != null ? currentTarget.AimPoint : currentTarget.transform);
        markerInstance.SetVisible(true);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = targetingOrigin != null ? targetingOrigin.position : transform.position;

        Gizmos.color = autoTargetingEnabled
            ? new Color(1f, 0.85f, 0.2f, 0.85f)
            : new Color(0.45f, 0.45f, 0.45f, 0.75f);

        Gizmos.DrawWireSphere(origin, searchRadius);

        if (currentTarget != null && currentTarget.AimPoint != null)
        {
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.95f);
            Gizmos.DrawLine(origin, currentTarget.AimPoint.position);
            Gizmos.DrawWireSphere(currentTarget.AimPoint.position, 0.08f);
        }
    }
#endif
}