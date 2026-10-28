using System;

// Data-only dependency of the copied catalog; no LiarPainter gameplay systems are imported.
[Serializable]
public sealed class LiarPainterRoundContent
{
    public string citizenKeyword = string.Empty;
    public string drunkPainterKeyword = string.Empty;
    public string question = string.Empty;
}
