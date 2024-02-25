using UnityEngine;

public class Metronome : MonoBehaviour
{
    [SerializeField, Tooltip("In beats per minute")]
    private uint _tempo = 100;

#pragma warning disable 649
    [SerializeField]
    private AudioSource _audio;
    [SerializeField]
    private AudioClip _metronomeSound;
#pragma warning restore 649

    public uint Tempo => _tempo;

    private void OnEnable()
    {
        // Sanity checks
        if (_tempo == 0)
        {
            Debug.LogError($"{nameof(Metronome)}: The '{nameof(_tempo)}' value has to be greater than 0. Disabling the script");
            enabled = false;
            return;
        }

        if (_audio == null)
        {
            Debug.LogError($"{nameof(Metronome)}: The '{nameof(_audio)}' field cannot be left unassigned. Disabling the script");
            enabled = false;
            return;
        }

        if (_metronomeSound == null)
        {
            Debug.LogError($"{nameof(Metronome)}: The '{nameof(_metronomeSound)}' field cannot be left unassigned. Disabling the script");
            enabled = false;
            return;
        }

        Restart();
    }

    private void Restart()
    {
        float repeatRate = 60f / _tempo;

        // Call the 'Tick' method in repeatRate seconds, then every repeatRate second
        InvokeRepeating("Tick", repeatRate, repeatRate);

        Tick();
    }

    private void Tick()
    {
        // We assume that this AudioSource is used not only by us, so a little cleaning wouldn't hurt
        _audio.Stop();
        _audio.clip = _metronomeSound;

        // Play the 'Metronome' sound
        _audio.Play();
    }

    private void OnDisable()
    {
        // Stop calling this method for now
        CancelInvoke();
    }
}

