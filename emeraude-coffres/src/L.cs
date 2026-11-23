namespace EmeraudeCoffres
{
    public enum Language { English, French }

    // Textes du mod : anglais par défaut, français si General.Language = French.
    static class L
    {
        public static bool Fr => Plugin.Lang != null && Plugin.Lang.Value == Language.French;
        public static string T(string en, string fr) => Fr ? fr : en;
    }
}
