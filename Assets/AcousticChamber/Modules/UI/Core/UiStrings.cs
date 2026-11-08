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

        // Stop dialog
        public const string StopTitle = "gpl.stop.title";
        public const string StopMessage = "gpl.stop.message";
        public const string StopMessageDebug = "gpl.stop.message_debug";
        public const string StopKeepGoing = "gpl.stop.keep_going";
        public const string StopDropOut = "gpl.stop.drop_out";
        public const string StopBackToMenu = "gpl.stop.back_to_menu";
        public const string StopConfirmTitle = "gpl.stop.confirm_title";
        public const string StopConfirmMessage = "gpl.stop.confirm_message";

        public static readonly StringTable Table = new StringTable("GPL.UI")
            .Add(Ok, "OK", "確定")
            .Add(Cancel, "Cancel", "取消")
            .Add(Back, "Back", "返回")
            .Add(Next, "Next", "下一步")
            .Add(Continue, "Continue", "繼續")
            .Add(Clear, "Clear", "清除")
            .Add(LookAhead, "Please look straight ahead", "請直視前方")
            .Add(Language, "Language", "語言")
            .Add(StopTitle, "Stop the session?", "要停止嗎？")
            .Add(StopMessage, "Everything is paused. Keep going continues where you left off. Drop out ends the session for good.",
                 "目前已暫停。「繼續」會從剛才的地方接著進行；「退出實驗」會結束本次實驗，之後無法再繼續。")
            .Add(StopMessageDebug, "Playground run, paused. Back to the chamber menu ends this run.",
                 "除錯模式，已暫停。「回到選單」會結束這次執行。")
            .Add(StopKeepGoing, "Keep going", "繼續")
            .Add(StopDropOut, "Drop out", "退出實驗")
            .Add(StopBackToMenu, "Back to the chamber menu", "回到選單")
            .Add(StopConfirmTitle, "Drop out {0}?", "確定要讓 {0} 退出實驗嗎？")
            .Add(StopConfirmMessage, "This session cannot be resumed.", "本次實驗結束後無法再繼續。");

        public static void EnsureRegistered() => Loc.Register(Table);
    }
}
