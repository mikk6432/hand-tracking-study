using UnityEngine;
using Parabox.CSG;

public class Ring : MonoBehaviour
{
    [SerializeField] public float radius;
    [SerializeField] public float width;
    [SerializeField] public float height;
    [SerializeField] public float lengthOfC;
    [SerializeField]
    public Material material;

    private GameObject ringGameObject;
    public void Render()
    {
        if (ringGameObject != null)
            Destroy(ringGameObject);
        // Initialize two new meshes in the scene
        GameObject outerCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        GameObject innerCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outerCylinder.transform.localScale = new Vector3(radius * 2 + width, height / 2, radius * 2 + width);
        innerCylinder.transform.localScale = new Vector3(radius * 2 - width, height / 2, radius * 2 - width);

        // Perform boolean operation
        Model result = CSG.Subtract(outerCylinder, innerCylinder);
        Destroy(outerCylinder);
        Destroy(innerCylinder);

        // Create a gameObject to render the result
        var circleGameObject = new GameObject();
        circleGameObject.AddComponent<MeshFilter>().sharedMesh = result.mesh;
        circleGameObject.AddComponent<MeshRenderer>().material = material;

        GameObject CCutout = GameObject.CreatePrimitive(PrimitiveType.Cube);
        CCutout.transform.localScale = new Vector3(lengthOfC, height, radius + width);
        CCutout.transform.localPosition = new Vector3(0, 0, -(radius + width) / 2);
        Model result2 = CSG.Subtract(circleGameObject, CCutout);
        Destroy(circleGameObject);
        Destroy(CCutout);

        ringGameObject = new GameObject();
        ringGameObject.AddComponent<MeshFilter>().sharedMesh = result2.mesh;
        ringGameObject.AddComponent<MeshRenderer>().material = material;
        ringGameObject.transform.parent = transform;
        ringGameObject.transform.localPosition = Vector3.zero;
    }

    private void Start()
    {
        Render();
    }

}