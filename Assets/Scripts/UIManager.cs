using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Slider waveProgressBar;
    public TextMeshProUGUI waveText;

    public void UpdateWaveText(int currentWave, int totalWaves)
    {
        waveText.text = "ВОЛНА " + currentWave + "/" + totalWaves;
    }

    public void UpdateProgressBar(float progress)
    {
        waveProgressBar.value = progress;
    }
}