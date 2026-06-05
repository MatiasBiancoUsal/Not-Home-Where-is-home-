using UnityEngine;
using UnityEngine.Audio; // para usar el AudioMixer
using UnityEngine.UI;    // para usar los Slider

public class VolumeSettings : MonoBehaviour
{
    [Header("Referencias")]
    public AudioMixer audioMixer; // arrastrar aca el MainMixer
    public Slider musicSlider;    // arrastrar el slider de Musica
    public Slider sfxSlider;      // arrastrar el slider de Sonidos

    private void Start()
    {
        // Si hay un volumen guardado de antes lo usamos; si no, arrancamos en 1 (el maximo).
        float music = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Ponemos cada slider en su lugar SIN disparar el evento (para no aplicar el volumen dos veces).
        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);

        // Aplicamos el volumen guardado al mixer.
        SetMusicVolume(music);
        SetSFXVolume(sfx);

        // Conectamos los sliders por codigo: cuando los moves, se llaman solos estos metodos.
        // (Asi NO hace falta configurar el "On Value Changed" a mano en el Inspector.)
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    // Se llama desde el evento "On Value Changed" del slider de MUSICA.
    public void SetMusicVolume(float value)
    {
        // El mixer trabaja en decibeles (escala logaritmica), por eso convertimos el 0-1 del slider.
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat("MusicVolume", value); // guardamos para la proxima vez
    }

    // Se llama desde el evento "On Value Changed" del slider de SONIDOS.
    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }
}
