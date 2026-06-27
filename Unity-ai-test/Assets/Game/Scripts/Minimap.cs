using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight OnGUI radar in the bottom-right corner. Plots the player, active
/// police, remaining loot and the extraction zone relative to the player so it
/// is easy to orient in the neon city.
/// </summary>
public class Minimap : MonoBehaviour
{
    public float worldRadius = 70f;
    public float size = 180f;
    public float margin = 16f;

    public Vector3 extraction = new Vector3(40f, 0f, 40f);
    public bool showExtraction = true;

    Texture2D _dot;

    void EnsureDot()
    {
        if (_dot != null) return;
        _dot = Texture2D.whiteTexture;
    }

    void OnGUI()
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        EnsureDot();

        float s = size;
        Rect area = new Rect(Screen.width - s - margin, Screen.height - s - margin, s, s);

        // Background panel.
        GUI.color = new Color(0.02f, 0.02f, 0.06f, 0.65f);
        GUI.DrawTexture(area, _dot);
        GUI.color = new Color(0.2f, 1f, 0.9f, 0.5f);
        DrawBorder(area, 2f);

        Vector3 origin = player.transform.position;
        Vector2 center = new Vector2(area.x + s * 0.5f, area.y + s * 0.5f);

        // Extraction.
        if (showExtraction)
            PlotDot(center, origin, extraction, new Color(0.3f, 1f, 0.5f), 7f);

        // Loot.
        foreach (var p in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            PlotDot(center, origin, p.transform.position, new Color(1f, 0.85f, 0.2f), 5f);

        // Police (only the live ones with renderers enabled).
        var gm = GameManager.Instance;
        if (gm != null && gm.wanted > 0)
        {
            foreach (var cop in Object.FindObjectsByType<PoliceAI>(FindObjectsSortMode.None))
            {
                var r = cop.GetComponentInChildren<Renderer>();
                if (r != null && r.enabled)
                    PlotDot(center, origin, cop.transform.position, new Color(1f, 0.25f, 0.4f), 5f);
            }
        }

        // Player (always centered).
        DrawCenteredDot(center, new Color(0.3f, 0.9f, 1f), 7f);

        GUI.color = Color.white;
    }

    void PlotDot(Vector2 center, Vector3 origin, Vector3 world, Color color, float r)
    {
        Vector3 rel = world - origin;
        Vector2 p = new Vector2(rel.x, rel.z) / worldRadius * (size * 0.5f);
        if (p.magnitude > size * 0.5f - 4f) p = p.normalized * (size * 0.5f - 4f);
        // +z (north) should map upward, so invert y.
        DrawCenteredDot(new Vector2(center.x + p.x, center.y - p.y), color, r);
    }

    void DrawCenteredDot(Vector2 c, Color color, float r)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(c.x - r * 0.5f, c.y - r * 0.5f, r, r), _dot);
    }

    void DrawBorder(Rect r, float t)
    {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), _dot);
        GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), _dot);
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), _dot);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), _dot);
    }
}
