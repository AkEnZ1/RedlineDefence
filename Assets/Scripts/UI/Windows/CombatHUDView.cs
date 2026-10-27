using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Делаем боевой HUD окном нашего UIManager
public class CombatHUDView : UIWindow
{
    [Header("Wave Info")]
    [SerializeField] private Slider waveProgressBar;
    [SerializeField] private TextMeshProUGUI waveText;

    public void UpdateWaveText(int currentWave, int totalWaves)
    {
        if (waveText != null)
            waveText.text = $"ВОЛНА {currentWave}/{totalWaves}";
    }

    public void UpdateProgressBar(float progress)
    {
        if (waveProgressBar != null)
            waveProgressBar.value = progress;
    }
}