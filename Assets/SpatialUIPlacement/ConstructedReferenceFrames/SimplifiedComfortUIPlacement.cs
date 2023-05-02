using UnityEngine;

namespace SpatialUIPlacement
{
    public class SimplifiedComfortUIPlacement : MonoBehaviour
    {
        private static readonly float HEIGHT_PERCENT = 75f;

        [SerializeField] private Transform _headset;

        private void Start()
        {
            if (_headset == null && (_headset = Camera.main.transform) == null)
            {
                Debug.LogError(
                    $"{nameof(SimplifiedComfortUIPlacement)}: The '{nameof(_headset)}' field cannot be left unassigned. Disabling the script");
                enabled = false;
                return;
            }
        }

        public void Refresh()
        {
            if (!_headset) return;
            transform.position = new Vector3(0, _headset.position.y * HEIGHT_PERCENT / 100, 0);
        }
    }
}