using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Math = Utils.Math;

public class TargetsManager : MonoBehaviour
{
    private static readonly Color _activeColor = Color.black;
    private static readonly Color _inactiveColor = Color.white;
    private static readonly Color _successColor = Color.green;
    private static readonly Color _failColor = Color.red;
    private const float diameter = .15f;
    public const int TargetsCount = 7;

    public readonly UnityEvent selectorEnteredTargetsZone = new();
    public readonly UnityEvent selectorExitedTargetsZone = new();

    private List<GameObject> _targets;
    private Dictionary<GameObject, Renderer> _targetToRendererComponentMap;
    
    [SerializeField] private GameObject targetPrefab;

    private TargetSizeVariant _targetSize = TargetSizeVariant.Medium;
    
    public bool IsShowingTargets { get; private set; }
    public bool IsSelectorInsideCollider { get; private set; }
    public (GameObject target, int targetIndex) ActiveTarget { get; private set; } = (null, -1);
    public SelectionDonePayload LastSelectionData { get; private set; }
    public GameObject Anchor { get; set; }
    public TargetSizeVariant TargetSize
    {
        get => _targetSize;
        set
        {
            if (value == _targetSize) return;
            
            _targets.ForEach(target =>
            {
                target.transform.localScale = new Vector3(
                    GetTargetDiameter(value), 
                    GetTargetDiameter(value), 
                    0.001f
                );
            });
            
            _targetSize = value;
        }
    }

    public enum TargetSizeVariant
    {
        Small,
        Medium,
        Big,
        VeryBig
    }
    
    public class SelectionDonePayload
    {
        public readonly int activeTargetIndex;
        public readonly float targetSize;
        public readonly Vector2 targetAbsoluteCoordinates;
        public readonly Vector2 selectionAbsoluteCoordinates;
        public readonly Vector2 selectionLocalCoordinates;
        public readonly bool success;

        public SelectionDonePayload(int activeTargetIndex, float targetSize, Vector2 targetAbsoluteCoordinates, Vector2 selectionAbsoluteCoordinates, Vector2 selectionLocalCoordinates, bool success)
        {
            this.activeTargetIndex = activeTargetIndex;
            this.targetSize = targetSize;
            this.targetAbsoluteCoordinates = targetAbsoluteCoordinates;
            this.selectionAbsoluteCoordinates = selectionAbsoluteCoordinates;
            this.selectionLocalCoordinates = selectionLocalCoordinates;
            this.success = success;
        }
    }
    
    private void OnEnable()
    {
        if (!targetPrefab)
        {
            Debug.LogError(
                $"TargetsManager: the '{nameof(targetPrefab)}' object is not set. Disabling the script.");
            enabled = false;
            return;
        }
    }

    private void Awake()
    {
        _targets = new List<GameObject>(TargetsCount);
        _targetToRendererComponentMap = new(TargetsCount);
        
        for (int i = 0; i < TargetsCount; i++)
        {
            var target = Instantiate(targetPrefab, transform);

            // trigonometry to move to position on the circle
            var localPosition = CalcTargetLocalPosition(i);

            // scaling only X and Y
            var localScale = new Vector3(GetTargetDiameter(TargetSize), GetTargetDiameter(TargetSize), 0.001f);

            target.transform.localScale = localScale;
            target.transform.localPosition = localPosition;

            var rendererComponent = target.transform.Find("Cylinder").GetComponent<Renderer>();

            rendererComponent.material.color = _inactiveColor;
            rendererComponent.enabled = false; // not showing after creating

            _targets.Add(target);
            _targetToRendererComponentMap[target] = rendererComponent;
        }
    }

    public static float GetTargetDiameter(TargetSizeVariant targetSize)
    {
        return targetSize == TargetSizeVariant.VeryBig ? 0.05f : 
            targetSize == TargetSizeVariant.Big ? 0.04f : 
            targetSize == TargetSizeVariant.Medium ? 0.03f :
            0.02f; /*TargetSize == TargetSizeVariant.Small*/
    }

    private static Vector3 CalcTargetLocalPosition(int targetIndex)
    {
        var angle = targetIndex * (2 * Mathf.PI / TargetsCount);
        return new Vector3(
            Mathf.Cos(angle) * diameter / 2,
            Mathf.Sin(angle) * diameter / 2,
            0
        );
    }

    private void Update()
    {
        if (Anchor && Anchor.activeInHierarchy)
            transform.SetPositionAndRotation(Anchor.transform.position, Anchor.transform.rotation);
    }

    public void EnsureTargetsShown()
    {
        if (IsShowingTargets) return;
        _targets.ForEach(target => _targetToRendererComponentMap[target].enabled = true);
        IsShowingTargets = true;
    }
    
    public void EnsureTargetsHidden()
    {
        if (!IsShowingTargets) return;
        _targets.ForEach(target => _targetToRendererComponentMap[target].enabled = false);

        EnsureNoActiveTargets();
        
        IsShowingTargets = false;
    }

    public void ActivateTarget(int targetIndex)
    {
        if (targetIndex >= TargetsCount) throw new ArgumentException($"Invalid target index = {targetIndex}");
        if (!IsShowingTargets) throw new InvalidOperationException("Cannot activate target when target are not being showed");
        EnsureNoActiveTargets();

        var target = _targets[targetIndex];
        ActiveTarget = (target, targetIndex);
        
        _targetToRendererComponentMap[target].material.color = _activeColor;
    }

    public void EnsureNoActiveTargets()
    {
        bool needToDeactivateActiveTarget = ActiveTarget.targetIndex != -1;
        if (needToDeactivateActiveTarget)
        {
            _targetToRendererComponentMap[ActiveTarget.target].material.color = _inactiveColor;
            ActiveTarget = (null, -1);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Selector")) return;
        IsSelectorInsideCollider = true;
        
        if (!IsShowingTargets) return;
        if (ActiveTarget.targetIndex == -1) return;
        
        // plenty of math then
        var projection = Math.ProjectPointOntoOXYPlane(transform, other.transform.position);
        var targetLocalPosition = CalcTargetLocalPosition(ActiveTarget.targetIndex);

        var targetAbsoluteCoordinates = new Vector2(
            targetLocalPosition.x,
            targetLocalPosition.y
        );
        var selectionAbsoluteCoordinates = new Vector2(
            projection.local.x,
            projection.local.y
        );
        var selectionLocalCoordinates = selectionAbsoluteCoordinates - targetAbsoluteCoordinates;
        var success = selectionLocalCoordinates.magnitude < (GetTargetDiameter(TargetSize) / 2);

        _targetToRendererComponentMap[ActiveTarget.target].material.color =
            success ? _successColor : _failColor;
        
        var payload = new SelectionDonePayload(
            ActiveTarget.targetIndex,
            GetTargetDiameter(TargetSize),
            targetAbsoluteCoordinates,
            selectionAbsoluteCoordinates,
            selectionLocalCoordinates,
            success
        );

        LastSelectionData = payload;
        
        selectorEnteredTargetsZone.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Selector")) return; 
        IsSelectorInsideCollider = false;
        
        if (!IsShowingTargets) return;
        if (ActiveTarget.targetIndex == -1) return;
        
        var rendererComponent = _targetToRendererComponentMap[ActiveTarget.target];
        // a bit dirty solution. Used, when at the moment of activating the target, selector was inside the collider
        bool isNotSelectionEnd = rendererComponent.material.color == _activeColor;
        if (isNotSelectionEnd) return; // do nothing in this situation and wait for the selection
        
        rendererComponent.material.color = _inactiveColor;

        ActiveTarget = (null, -1);
        
        selectorExitedTargetsZone.Invoke();
    }
}