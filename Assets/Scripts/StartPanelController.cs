using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartPanelController : MonoBehaviour
{
    public static bool IsStartPanelOpen = true;

    public GameObject startPanel;
    public TMP_InputField nameInput;
    public Button startButton;

    void Start()
    {
        IsStartPanelOpen = true;
        startPanel.SetActive(true);
        startButton.onClick.AddListener(OnStartClicked);
    }

    void OnStartClicked()
    {
        string name = string.IsNullOrWhiteSpace(nameInput.text) ? "Anonymous" : nameInput.text.Trim();
        GameManager.CurrentPlayerName = name;
        Debug.Log($"[Start] Player name set to: {name}");
        startPanel.SetActive(false);
        IsStartPanelOpen = false;
    }
}