using UnityEngine;

public class ErrorIndicatorManager : MonoBehaviour
{
#pragma warning disable 649
    [SerializeField]
    private AudioSource _audio;
    [SerializeField]
    private AudioClip _errorSound;
#pragma warning restore 649

    [Space]
    [SerializeField, Tooltip("How long the overlay will be displayed in seconds")]
    private float _displayPeriod = 0.5f;

    private void OnEnable()
    {
        // Sanity checks
        if (_audio == null)
        {
            Debug.LogError($"{nameof(ErrorIndicatorManager)}: The '{nameof(_audio)}' field cannot be left unassigned. Disabling the script");
            enabled = false;
            return;
        }

        if (_errorSound == null)
        {
            Debug.LogError($"{nameof(ErrorIndicatorManager)}: The '{nameof(_errorSound)}' field cannot be left unassigned. Disabling the script");
            enabled = false;
            return;
        }

        Invoke(nameof(OnCountdownFinish), _displayPeriod);
        PlaySound();
    }

    private void PlaySound()
    {
        // We assume that this AudioSource is used not only by us, so a little cleaning wouldn't hurt
        _audio.Stop();
        _audio.clip = _errorSound;

        // Play the 'Metronome' sound
        _audio.Play();
    }

    private void OnCountdownFinish()
    {
        gameObject.SetActive(false);
    }
}
