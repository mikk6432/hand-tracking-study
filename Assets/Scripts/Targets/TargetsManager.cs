using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using System.Linq;

public class TargetsManager : MonoBehaviour
{
    private static readonly Color _activeColor = Color.black;
    private static readonly Color _activeHoverColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color _inactiveColor = Color.white;
    private static readonly Color _inactiveHoverColor = new Color(0.8f, 0.8f, 0.8f);
    private static readonly Color _successColor = Color.green;
    private static readonly Color _failColor = Color.red;
    [SerializeField] public float diameter = .15f; // Default: .15f
    [SerializeField] public const int TargetsCount = 7;

    public readonly UnityEvent selectorEnteredTargetsZone = new();
    public readonly UnityEvent selectorExitedTargetsZone = new();
    public readonly UnityEvent selectorExitedWrongSide = new();

    private List<GameObject> _targets;
    private Dictionary<GameObject, Renderer> _targetToRendererComponentMap;

    [SerializeField] private GameObject targetPrefab;

    private TargetSizeVariant _targetSize = TargetSizeVariant.Small;

    public bool IsShowingTargets { get; private set; }
    public bool IsSelectorInsideCollider { get; private set; }
    public (GameObject target, int targetIndex) ActiveTarget { get; private set; } = (null, -1);
    public SelectionDonePayload LastSelectionData { get; private set; }
    [SerializeField] public GameObject Anchor;
    [SerializeField]
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
#if UNITY_EDITOR
    public TargetSizeVariant SpecifiedTargetSize = TargetSizeVariant.Medium;
    private void OnValidate()
    {
        if (SpecifiedTargetSize != TargetSize)
        {
            TargetSize = SpecifiedTargetSize;
        }
    }
#endif

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

    [SerializeField] bool trial;

    private void Start()
    {
        if (trial)
        {
            EnsureTargetsShown();
            var targetsIndexesSequence = GenerateTargetsIndexesSequence();
            targetsIndexesSequence.MoveNext();
            ActivateTarget(targetsIndexesSequence.Current);
            selectorExitedTargetsZone.AddListener(() =>
            {
                if (targetsIndexesSequence.MoveNext())
                {
                    ActivateTarget(targetsIndexesSequence.Current);
                }
                else
                {
                    targetsIndexesSequence = GenerateTargetsIndexesSequence();
                    targetsIndexesSequence.MoveNext();
                    ActivateTarget(targetsIndexesSequence.Current);
                }
            });
            ChangeHandedness(Hand);
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
    [Serializable]
    public class TargetDiameter
    {
        public float small = 0.03f;
        public float medium = 0.04f;
        public float big = 0.05f;
        public float veryBig = 0.06f;
    }

    [SerializeField] private TargetDiameter targetDiameters = new();

    public float GetTargetDiameter(TargetSizeVariant targetSize)
    {
        return targetSize == TargetSizeVariant.VeryBig ? targetDiameters.veryBig :
            targetSize == TargetSizeVariant.Big ? targetDiameters.big :
            targetSize == TargetSizeVariant.Medium ? targetDiameters.medium :
            targetDiameters.small; /*TargetSize == TargetSizeVariant.Small*/
    }

    public enum Handed
    {
        Left,
        Right
    }

    [SerializeField] private Handed Hand = Handed.Right;
    [SerializeField] private GameObject[] LeftHands;
    [SerializeField] private GameObject[] RightHands;
    [SerializeField] private GameObject RightIndexFinger;
    [SerializeField] private GameObject LeftIndexFinger;

    public void ChangeHandedness(Handed Hand)
    {
        if (Hand == Handed.Left)
        {
            GetComponent<SelectorAnimatedProjector>().Selector = LeftIndexFinger.transform;
            RightIndexFinger.SetActive(false);
            LeftIndexFinger.SetActive(true);
            foreach (var leftHand in LeftHands)
            {
                leftHand.GetComponent<OVRMeshRenderer>().enabled = true;
                leftHand.GetComponent<SkinnedMeshRenderer>().enabled = true;
            }
            foreach (var rightHand in RightHands)
            {
                rightHand.GetComponent<OVRMeshRenderer>().enabled = false;
                rightHand.GetComponent<SkinnedMeshRenderer>().enabled = false;
            }
        }
        if (Hand == Handed.Right)
        {
            GetComponent<SelectorAnimatedProjector>().Selector = RightIndexFinger.transform;
            RightIndexFinger.SetActive(true);
            LeftIndexFinger.SetActive(false);
            foreach (var rightHand in RightHands)
            {
                rightHand.GetComponent<OVRMeshRenderer>().enabled = true;
                rightHand.GetComponent<SkinnedMeshRenderer>().enabled = true;
            }
            foreach (var leftHand in LeftHands)
            {
                leftHand.GetComponent<OVRMeshRenderer>().enabled = false;
                leftHand.GetComponent<SkinnedMeshRenderer>().enabled = false;
            }
        }
    }

    private Vector3 CalcTargetLocalPosition(int targetIndex)
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
        var selector = Hand == Handed.Right ? RightIndexFinger : LeftIndexFinger;
        var (world, local) = Math.ProjectPointOntoOXYPlane(transform, selector.transform.position);
        var fromSelectorToProjection = selector.transform.position - world;
        var distanceToOXY = fromSelectorToProjection.magnitude;
        var colliderBox = GetComponent<BoxCollider>().size;
        /* if (IsSelectorInsideCollider && (distanceToOXY > transform.position.z * 3 || !(Mathf.Abs(local.x) < colliderBox.x / 2 &&
                     Mathf.Abs(local.y) < colliderBox.y / 2)))
        {
            IsSelectorInsideCollider = false;
            if (ActiveTarget.targetIndex != -1)
            {
                _targetToRendererComponentMap[ActiveTarget.target].material.color = _inactiveColor;
                ActiveTarget = (null, -1);
                selectorExitedWrongSide.Invoke();
            }
        } */
        _targetToRendererComponentMap.ToList().ForEach(pair =>
        {
            var (target, renderer) = pair;
            var activeNow = target == ActiveTarget.target;
            var failNow = renderer.material.color == _failColor;
            var successNow = renderer.material.color == _successColor;
            var localPos = target.transform.localPosition;
            var hover = Mathf.Abs(local.x - localPos.x) < GetTargetDiameter(TargetSize) / 2 &&
                        Mathf.Abs(local.y - localPos.y) < GetTargetDiameter(TargetSize) / 2;
            renderer.material.color =
                failNow ? _failColor :
                successNow ? _successColor :
                activeNow ? hover ? _activeHoverColor : _activeColor :
                hover ? _inactiveHoverColor : _inactiveColor;
        });
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

    [SerializeField] private GameObject TargetCube;
    [SerializeField] private GameObject Outline;
    [SerializeField] private GameObject SelectorProjection;

    public void hideCube()
    {
        TargetCube.SetActive(false);
        Outline.SetActive(false);
        SelectorProjection.SetActive(false);
    }

    public void showCube()
    {
        TargetCube.SetActive(true);
        Outline.SetActive(true);
        SelectorProjection.SetActive(true);
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

    public readonly UnityEvent click = new();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Selector")) return;

        if (!IsShowingTargets) return;
        if (ActiveTarget.targetIndex == -1) return;

        //if (IsSelectorInsideCollider) return;
        IsSelectorInsideCollider = true;

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

        click.Invoke();

        LastSelectionData = payload;

        IsSelectorInsideCollider = true;

        selectorEnteredTargetsZone.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Selector")) return;

        //if (!IsSelectorInsideCollider) return;
        IsSelectorInsideCollider = false;

        if (!IsShowingTargets) return;
        if (ActiveTarget.targetIndex == -1) return;

        /* var selectorPosition = other.transform.position;
        var (world, local) = Math.ProjectPointOntoOXYPlane(transform, selectorPosition);

        var fromSelectorToProjection = selectorPosition - world;

        var side = Vector3.Dot(transform.forward, fromSelectorToProjection) >= 0;
        if (!side)
        {
            SelectingEnded();
        } */
        SelectingEnded();
    }

    public void SelectingEnded()
    {
        var rendererComponent = _targetToRendererComponentMap[ActiveTarget.target];
        rendererComponent.material.color = _inactiveColor;
        ActiveTarget = (null, -1);
        IsSelectorInsideCollider = false;
        selectorExitedTargetsZone.Invoke();
    }

    public static IEnumerator<TargetSizeVariant> GenerateTargetSizesSequence(int seed, bool isTraining = false)
    {
        var random = new System.Random(seed);
        var seq = new List<TargetSizeVariant>
            {
                TargetSizeVariant.Small,
                TargetSizeVariant.Medium,
                TargetSizeVariant.Big,
                TargetSizeVariant.VeryBig,
            }
            .Select(size => new { size, rnd = random.Next() })
            .OrderBy(x => x.rnd)
            .Select(x => x.size)
            .ToList();

        if (!isTraining) return seq.GetEnumerator();

        IEnumerator<TargetSizeVariant> TrainingInfiniteEnumerator()
        {
            while (true)
            {
                foreach (var x in seq)
                    yield return x;
            }
            // ReSharper disable once IteratorNeverReturns
        }

        return TrainingInfiniteEnumerator();
    }

    public static IEnumerator<int> GenerateTargetsIndexesSequence()
    {
        return Math.FittsLaw(TargetsCount)
            .Take(TargetsCount)
            .GetEnumerator();
    }
}