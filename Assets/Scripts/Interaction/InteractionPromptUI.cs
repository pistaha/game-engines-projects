using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    // Рисует прицел в центре экрана и меняет цвет, когда под прицелом есть цель.
    [SerializeField] private float reticleSize = 7f;
    [SerializeField] private Color reticleColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color activeReticleColor = new Color(0.48f, 1f, 0.62f, 1f);

    private bool hasTarget;

    public void SetPrompt(string text, bool targetAvailable)
    {
        hasTarget = targetAvailable;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        DrawReticle();
    }

    private void DrawReticle()
    {
        float x = (Screen.width - reticleSize) * 0.5f;
        float y = (Screen.height - reticleSize) * 0.5f;
        Rect rect = new Rect(x, y, reticleSize, reticleSize);

        Color previousColor = GUI.color;
        GUI.color = hasTarget ? activeReticleColor : reticleColor;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

}
