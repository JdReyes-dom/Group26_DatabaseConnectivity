using UnityEngine;
using UnityEngine.EventSystems;

public class LaneClick : MonoBehaviour
{
    public int laneIndex; // Set this in the Inspector (0-5)

    void OnMouseDown()
    {
        // Block clicks that are over any UI element (InputField, Button, Panel, etc.)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log($"[LaneClick] Click blocked — pointer over UI.");
            return;
        }

        // Also block while the StartPanel is still visible
        if (StartPanelController.IsStartPanelOpen)
        {
            Debug.Log("[LaneClick] Click blocked — StartPanel is open.");
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.DeployAlly(laneIndex);
        else
            Debug.LogError("GameManager not found!");
    }
}