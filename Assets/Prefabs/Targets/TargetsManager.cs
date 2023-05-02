using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Math = Utils.Math;

public class TargetsManager : MonoBehaviour
{
    private static readonly Color _activeColor = Color.black;
    private static readonly Color _inactiveColor = Color.gray;
    private static readonly Color _successColor = Color.green;
    private static readonly Color _failColor = Color.red;
    public const int TargetsCount = 7;
    private const float diameter = .15f;
    private const double MINIMUM_TIME_BETWEEN_SELECTIONS = .1; // in seconds

    [SerializeField] private GameObject targetPrefab;

    private bool _showing;
    private List<GameObject> _targets;
    private Dictionary<GameObject, Renderer> _targetToRendererComponentMap;

    public (GameObject target, int targetIndex) ActiveTarget { get; private set; } = (null, -1);
    public bool IsSelectorInsideCollider { get; private set; }
    public GameObject Anchor { get; set; }
    public TargetSizeVariant TargetSize { get; set; }

    private float TargetDiameter =>
        TargetSize == TargetSizeVariant.Big ? 0.035f :
        TargetSize == TargetSizeVariant.Medium ? 0.025f :
        0.015f; /*TargetSize == TargetSizeVariant.Small*/
    
    private DateTime lastSelectionDate = DateTime.Now;
    public SelectionDonePayload LastSelection { get; private set; }


    public readonly UnityEvent selectorEnteredTargetsZone = new();
    public readonly UnityEvent selectorExitedTargetsZone = new();

    public enum TargetSizeVariant
    {
        Small,
        Medium,
        Big
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
                $"{nameof(TargetsManager)}: the '{nameof(targetPrefab)}' object is not set. Disabling the script.");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (Anchor && Anchor.activeInHierarchy)
        {
            transform.SetPositionAndRotation(Anchor.transform.position, Anchor.transform.rotation);
        }
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

    public bool IsShowingTargets()
    {
        return _showing;
    }

    public void ShowTargets()
    {
        if (_showing)
            throw new InvalidOperationException(
                $"{nameof(TargetsManager)}: cannot call method ShowTargets when showing is already in progress"
            );

        _targets = new List<GameObject>(TargetsCount);
        _targetToRendererComponentMap = new(TargetsCount);

        // creating targets
        for (int i = 0; i < TargetsCount; i++)
        {
            var target = Instantiate(targetPrefab, transform);

            // trigonometry to move to position on the circle
            var localPosition = CalcTargetLocalPosition(i);

            // scaling only X and Y
            var localScale = new Vector3(TargetDiameter, TargetDiameter, 0.001f);

            target.transform.localScale = localScale;
            target.transform.localPosition = localPosition;

            var rendererComponent = target.transform.Find("Cylinder").GetComponent<Renderer>();

            rendererComponent.material.color = _inactiveColor;

            _targets.Add(target);
            _targetToRendererComponentMap[target] = rendererComponent;
        }

        _showing = true;
    }

    public void HideTargets()
    {
        if (!_showing)
            throw new InvalidOperationException(
                $"{nameof(TargetsManager)}: cannot call method HideTargets when showing is not in progress"
            );

        _targets.ForEach(Destroy);

        _targets = null;
        _targetToRendererComponentMap = null;
        ActiveTarget = (null, -1);
        LastSelection = null;
        _showing = false;
    }

    public void ActivateFirstTarget()
    {
        if (!_showing)
            throw new InvalidOperationException(
                $"{nameof(TargetsManager)}: cannot call method ActivateFirstTarget when showing is not in progress"
            );

        bool alreadyShownFirstTarget = ActiveTarget.targetIndex != -1;
        if (alreadyShownFirstTarget) 
            throw new InvalidOperationException(
                $"{nameof(TargetsManager)}: cannot call method ActivateFirstTarget when first target is already shown"
            );
        
        var firstActiveTarget = _targets[0];
        ActiveTarget = (firstActiveTarget, 0);
        
        _targetToRendererComponentMap[firstActiveTarget].material.color = _activeColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Selector")) return; 
        IsSelectorInsideCollider = true;
        
        if (!_showing) return;
        if (ActiveTarget.targetIndex == -1) return; // equivalent of ActiveTarget.target == null

        var now = DateTime.Now;
        if (now.Subtract(lastSelectionDate).Milliseconds / 1000.0 < MINIMUM_TIME_BETWEEN_SELECTIONS) return;
        lastSelectionDate = now;

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
        var success = selectionLocalCoordinates.magnitude < (TargetDiameter / 2);

        _targetToRendererComponentMap[ActiveTarget.target].material.color =
            success ? _successColor : _failColor;
        
        var payload = new SelectionDonePayload(
            ActiveTarget.targetIndex,
            TargetDiameter,
            targetAbsoluteCoordinates,
            selectionAbsoluteCoordinates,
            selectionLocalCoordinates,
            success
        );

        LastSelection = payload;

        selectorEnteredTargetsZone.Invoke();
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Selector")) return; 
        IsSelectorInsideCollider = false;
        
        if (!_showing) return;
        if (ActiveTarget.targetIndex == -1) return; // equivalent of ActiveTarget.target == null

        var renderer = _targetToRendererComponentMap[ActiveTarget.target];

        // a bit dirty solution. Used, when at the moment of activating first target, selector was inside the collider
        bool isNotSelectionEnd = renderer.material.color == _activeColor;
        if (isNotSelectionEnd) return; // do nothing in this situation and wait for the selection
        /* else interpret this exit as a signal to activate next target */
        
        renderer.material.color = _inactiveColor;
        
        // Fitts Law here ;)
        int nextTargetIndex = (ActiveTarget.targetIndex + (TargetsCount / 2)) % TargetsCount;
        bool activateNext = nextTargetIndex != 0;

        if (activateNext)
        {
            ActiveTarget = (_targets[nextTargetIndex], nextTargetIndex);
            _targetToRendererComponentMap[ActiveTarget.target].material.color = _activeColor;
        }
        else
        {
            ActiveTarget = (null, -1);
        }
        selectorExitedTargetsZone.Invoke();
    }
}