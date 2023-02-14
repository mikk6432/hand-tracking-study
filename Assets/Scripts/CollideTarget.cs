using UnityEngine;
using UnityEngine.Events;

public class CollideTarget : MonoBehaviour
{
    public readonly UnityEvent selectEvent = new();
    private bool _isActive;
    private Renderer _renderer;

    private readonly Color _activeColor = Color.green;
    private readonly Color _inactiveColor = Color.red;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        SetColor(_inactiveColor);
    }
    
    public void MakeActive()
    {
        _isActive = true;
        SetColor(_activeColor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return;
        if (!other.gameObject.CompareTag("Selector")) return;

        _isActive = false;
        SetColor(_inactiveColor);
        selectEvent.Invoke();
    }

    private void SetColor(Color color)
    {
        _renderer.material.color = color;
    }
}
