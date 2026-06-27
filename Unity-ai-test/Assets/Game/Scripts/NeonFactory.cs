using UnityEngine;

/// <summary>
/// Central helper that builds URP neon materials at runtime so colors/emission stay consistent.
/// </summary>
public static class NeonFactory
{
    static Shader _lit;
    static Shader _unlit;

    static Shader Lit => _lit != null ? _lit : (_lit = Shader.Find("Universal Render Pipeline/Lit"));
    static Shader Unlit => _unlit != null ? _unlit : (_unlit = Shader.Find("Universal Render Pipeline/Unlit"));

    /// <summary>Solid lit surface with an emissive glow (drives Bloom).</summary>
    public static Material Lit_(Color baseColor, Color emission, float emissionIntensity = 2f, float smoothness = 0.5f)
    {
        var m = new Material(Lit);
        m.SetColor("_BaseColor", baseColor);
        m.SetFloat("_Smoothness", smoothness);
        if (emissionIntensity > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emission * emissionIntensity);
        }
        return m;
    }

    /// <summary>Flat surface, no emission (e.g. asphalt).</summary>
    public static Material Plain(Color baseColor, float smoothness = 0.2f)
    {
        var m = new Material(Lit);
        m.SetColor("_BaseColor", baseColor);
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    /// <summary>Unlit bright material for tracers/markers.</summary>
    public static Material Glow(Color color)
    {
        var m = new Material(Unlit);
        m.SetColor("_BaseColor", color);
        return m;
    }

    public static Material TracerMaterial(Color color)
    {
        return Glow(color);
    }
}
