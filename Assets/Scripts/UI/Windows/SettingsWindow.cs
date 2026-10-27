using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsWindow : UIWindow
{
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer mainAudioMixer; // Ссылка на AudioMixer (если используется)
    [SerializeField] private Slider soundSlider;        // Ползунок громкости
    private const string SoundPrefKey = "GameMasterVolume";

    [Header("Brightness Settings")]
    [SerializeField] private Slider brightnessSlider;   // Ползунок яркости
    [SerializeField] private Image brightnessOverlay;   // Полупрозрачная черная панель поверх экрана для регулировки яркости
    private const string BrightnessPrefKey = "GameBrightness";

    [Header("Navigation")]
    [SerializeField] private Button backButton;         // Кнопка возврата в Главное меню

    protected override void Awake()
    {
        base.Awake();
        LoadSettings();
    }

    private void OnEnable()
    {
        if (soundSlider != null)
            soundSlider.onValueChanged.AddListener(OnSoundSliderChanged);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(OnBrightnessSliderChanged);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDisable()
    {
        if (soundSlider != null)
            soundSlider.onValueChanged.RemoveListener(OnSoundSliderChanged);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessSliderChanged);

        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    // Загрузка сохраненных параметров при старте игры
    private void LoadSettings()
    {
        // Громкость (по умолчанию 1.0, если сохранения нет)
        float savedSound = PlayerPrefs.GetFloat(SoundPrefKey, 1f);
        if (soundSlider != null) soundSlider.value = savedSound;
        ApplySound(savedSound);

        // Яркость (по умолчанию 0.5, если сохранения нет)
        float savedBrightness = PlayerPrefs.GetFloat(BrightnessPrefKey, 0.5f);
        if (brightnessSlider != null) brightnessSlider.value = savedBrightness;
        ApplyBrightness(savedBrightness);
    }

    private void OnSoundSliderChanged(float value)
    {
        ApplySound(value);
        PlayerPrefs.SetFloat(SoundPrefKey, value);
        PlayerPrefs.Save();
    }

    private void ApplySound(float value)
    {
        // Превращаем линейное значение слайдера (0.001 - 1) в децибелы для AudioMixer (-80dB до 0dB)
        if (mainAudioMixer != null)
        {
            float dB = value > 0.001f ? Mathf.Log10(value) * 20f : -80f;
            mainAudioMixer.SetFloat("MasterVolume", dB);
        }
        else
        {
            // Если микшера нет, управляем общей громкостью через AudioListener
            AudioListener.volume = value;
        }
    }

    private void OnBrightnessSliderChanged(float value)
    {
        ApplyBrightness(value);
        PlayerPrefs.SetFloat(BrightnessPrefKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyBrightness(float value)
    {
        if (brightnessOverlay != null)
        {
            // Меняем прозрачность черной панели от 0 (ярко) до 0.7 (темно)
            Color color = brightnessOverlay.color;
            color.a = Mathf.Lerp(0.6f, 0f, value); // Чем выше ползунок, тем меньше черного цвета (ярче)
            brightnessOverlay.color = color;
        }
    }

    private void OnBackClicked()
    {
        // Возвращаемся в Главное меню через UIManager
        UIManager.Instance.OpenWindow("MainMenu");
    }
}