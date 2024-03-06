using UnityEngine;

public class OculusControllerAnchor : MonoBehaviour
{
    [SerializeField] private GameObject[] ovrControllers;

    private void Update()
    {
        foreach (var ovrController in ovrControllers)
        {
            if (ovrController.activeInHierarchy && ovrController.activeSelf && ovrController.transform.position != Vector3.zero)
            {
                transform.SetPositionAndRotation(ovrController.transform.position, ovrController.transform.rotation);
                return;
            }
        }
    }
}