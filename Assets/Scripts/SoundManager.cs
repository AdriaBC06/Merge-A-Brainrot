using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance; // Para acceder desde cualquier script

    [Header("SFX")]
    public AudioClip Click;    // sonido al clicar un objeto o botón
    public AudioClip Fusion;   // sonido al fusionar Brainrots
    public AudioClip Moneda;   // sonido al recoger monedas

    [Header("Music")]
    public AudioClip MusicaFondo;  // música de fondo normal
    public AudioClip Mercado;       // música del mercado

    private AudioSource sfxSource;
    private AudioSource musicSource;

    void Awake()
    {
        // Singleton: solo un SoundManager en la escena
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Crear dos AudioSources: uno para SFX y otro para música
        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true; // música se repite automáticamente
    }

    // Funciones para reproducir sonidos
    public void PlayClick() => sfxSource.PlayOneShot(Click);
    public void PlayFusion() => sfxSource.PlayOneShot(Fusion);
    public void PlayMoneda() => sfxSource.PlayOneShot(Moneda);

    // Funciones para reproducir música
    public void PlayMusicaFondo()
    {
        musicSource.clip = MusicaFondo;
        musicSource.Play();
    }

    public void PlayMercado()
    {
        musicSource.clip = Mercado;
        musicSource.Play();
    }
}