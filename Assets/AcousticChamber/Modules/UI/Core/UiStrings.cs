namespace GPL.UI
{
    /// <summary>Participant-facing strings used by GPL.UI itself. Registered by <see cref="EnsureRegistered"/>.</summary>
    public static class UiStrings
    {
        public const string Ok = "gpl.ok";
        public const string Cancel = "gpl.cancel";
        public const string Back = "gpl.back";
        public const string Next = "gpl.next";
        public const string Continue = "gpl.continue";
        public const string Clear = "gpl.clear";
        public const string LookAhead = "gpl.look_ahead";
        public const string Language = "gpl.language";

        public static readonly StringTable Table = new StringTable("GPL.UI")
            .Add(Ok, "OK", "確定")
            .Add(Cancel, "Cancel", "取消")
            .Add(Back, "Back", "返回")
            .Add(Next, "Next", "下一步")
            .Add(Continue, "Continue", "繼續")
            .Add(Clear, "Clear", "清除")
            .Add(LookAhead, "Please look straight ahead", "請直視前方")
            .Add(Language, "Language", "語言");

        public static void EnsureRegistered() => Loc.Register(Table);
    }
}
