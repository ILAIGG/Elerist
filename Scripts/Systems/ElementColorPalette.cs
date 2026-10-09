using Godot;

public static class ElementColorPalette
{
    public static Color GetColor(Element element)
    {
        return element switch
        {
            Element.Fire => new Color(1f, 0.38f, 0.2f),
            Element.Water => new Color(0.25f, 0.7f, 0.95f),
            Element.Earth => new Color(0.78f, 0.62f, 0.38f),
            Element.Wind => new Color(0.55f, 0.85f, 0.75f),
            Element.Plant => new Color(0.38f, 0.82f, 0.32f),
            Element.Lightning => new Color(1f, 0.82f, 0.25f),
            Element.Ice => new Color(0.55f, 0.82f, 1f),
            _ => Colors.White
        };
    }

    public static Color GetReactionColor(ElementalReaction reaction)
    {
        return reaction switch
        {
            ElementalReaction.Vaporization => new Color(0.72f, 0.76f, 0.8f),
            ElementalReaction.Freezing => GetColor(Element.Ice),
            _ => Colors.White
        };
    }
}