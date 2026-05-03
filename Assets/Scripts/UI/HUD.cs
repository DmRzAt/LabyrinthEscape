using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
    [Header("HP")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    [Header("Stamina")]
    public Slider staminaSlider;
    public TextMeshProUGUI staminaText;

    [Header("Keys")]
    public TextMeshProUGUI keysText;

    [Header("Messages")]
    public TextMeshProUGUI messageText;

    void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateHP;
        PlayerStamina.OnStaminaChanged += UpdateStamina;
        GameManager.OnKeysChanged   += UpdateKeys;
        GameManager.OnGameWon       += ShowWin;
        GameManager.OnGameLost      += ShowLose;
    }

    void Start()
    {
        if (GameManager.Instance != null)
            UpdateKeys(GameManager.Instance.keysCollected, GameManager.Instance.keysTotal);
    }

    void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateHP;
        PlayerStamina.OnStaminaChanged -= UpdateStamina;
        GameManager.OnKeysChanged   -= UpdateKeys;
        GameManager.OnGameWon       -= ShowWin;
        GameManager.OnGameLost      -= ShowLose;
    }

    void UpdateHP(int current, int max)
    {
        if (hpSlider != null) { hpSlider.maxValue = max; hpSlider.value = current; }
        if (hpText   != null) hpText.text = $"HP {current}/{max}";
    }

    void UpdateStamina(float current, float max)
    {
        if (staminaSlider != null) { staminaSlider.maxValue = max; staminaSlider.value = current; }
        if (staminaText != null) staminaText.text = $"SP {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    void UpdateKeys(int collected, int total)
    {
        if (keysText != null) keysText.text = $"Keys {collected}/{total}";
    }

    void ShowWin()  => ShowMessage("YOU WIN!");
    void ShowLose() => ShowMessage("YOU DIED...");

    void ShowMessage(string msg)
    {
        if (messageText != null) { messageText.text = msg; messageText.gameObject.SetActive(true); }
    }
}
