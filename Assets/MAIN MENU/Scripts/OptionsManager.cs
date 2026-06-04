using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OptionsManager : MonoBehaviour
{
    public Slider musicSlider;
    public Slider soundSlider;

    private void Start()
    {
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        soundSlider.value = PlayerPrefs.GetFloat("SoundVolume", 1f);

        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        soundSlider.onValueChanged.AddListener(AudioManager.Instance.SetSoundVolume);
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}