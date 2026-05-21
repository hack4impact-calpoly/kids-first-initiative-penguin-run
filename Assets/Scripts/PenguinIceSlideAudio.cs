using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PenguinIceSlideAudio : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip slideClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.55f;
    [SerializeField] private float minSlideSpeed = 1f;

    [Header("Ice Detection")]
    [SerializeField] private PhysicsMaterial2D iceTrackMaterial;
    [SerializeField] private LayerMask trackLayers = ~0;
    [SerializeField] private string[] iceNameKeywords = { "Ice", "TrackPieceBase", "Ramp" };
    [SerializeField] private string[] excludedNameKeywords = { "Sticky", "Gravel" };

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private bool touchingIceThisStep;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        EnsureAudioSource();
    }

    private void OnDisable()
    {
        StopSlideAudio();
    }

    private void FixedUpdate()
    {
        bool moving = rb != null && rb.simulated && rb.linearVelocity.magnitude >= minSlideSpeed;
        bool shouldPlay = touchingIceThisStep && moving && !DialogueManager.IsDialogueOpen;

        if (shouldPlay)
        {
            PlaySlideAudio();
        }
        else
        {
            StopSlideAudio();
        }

        touchingIceThisStep = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TrackIceContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TrackIceContact(collision);
    }

    private void TrackIceContact(Collision2D collision)
    {
        if (collision != null && IsIceTrack(collision.collider))
        {
            touchingIceThisStep = true;
        }
    }

    private bool IsIceTrack(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if ((trackLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return false;
        }

        string objectName = other.transform.root != null ? other.transform.root.name : other.name;
        if (ContainsKeyword(objectName, excludedNameKeywords))
        {
            return false;
        }

        if (iceTrackMaterial != null && other.sharedMaterial == iceTrackMaterial)
        {
            return true;
        }

        return ContainsKeyword(objectName, iceNameKeywords) || ContainsKeyword(other.name, iceNameKeywords);
    }

    private bool ContainsKeyword(string value, string[] keywords)
    {
        if (string.IsNullOrEmpty(value) || keywords == null)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(keywords[i]) && value.Contains(keywords[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void PlaySlideAudio()
    {
        if (slideClip == null)
        {
            return;
        }

        AudioSource source = EnsureAudioSource();
        source.clip = slideClip;
        source.volume = volume;
        source.loop = true;

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    private void StopSlideAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private AudioSource EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }
}
