using System;
using System.Collections.Generic;
using GPL.UI;
using UnityEngine;

namespace GPL.Protocol
{
    /// <summary>
    /// A deck of instruction slides for <see cref="ProtocolScreens.Slides"/>: per slide an id (logged), a title for the top
    /// bar and an illustration per language. Images are 2000 x 936 px (fills the 1000 x 468 u image area; keep text and
    /// key details 48 u = 96 px from the edges). A missing 中文 image or title falls back to English. An empty deck shows
    /// nothing.
    /// </summary>
    [CreateAssetMenu(menuName = "GPL/Protocol/Slide Set", fileName = "Slides")]
    public class SlideSet : ScriptableObject
    {
        [Serializable]
        public class Slide
        {
            [Tooltip("Logged in the slide row, e.g. procedure. Letters, digits, '_' and '-'.")]
            public string id = "slide";
            public string titleEnglish = "";
            [Tooltip("Empty = the English title.")]
            public string titleChinese = "";
            [Tooltip("Illustration 2000 x 936 px (Texture import: Non-Power of 2 = None, no mipmaps).")]
            public Texture2D imageEnglish;
            [Tooltip("Empty = the English image.")]
            public Texture2D imageChinese;

            [Tooltip("Label of the buttons that lead to this slide (\"← Practice\", \"Practice →\"). Empty = the title.")]
            public string shortEnglish = "";
            [Tooltip("Empty = the English short title, else the 中文 title.")]
            public string shortChinese = "";
            [Tooltip("Text shown on the gray image area while there is no image (e.g. a draft before the illustration exists).")]
            [TextArea] public string captionEnglish = "";
            [Tooltip("Empty = the English caption.")]
            [TextArea] public string captionChinese = "";

            public string Title(UiLanguage l) =>
                l == UiLanguage.Chinese && !string.IsNullOrEmpty(titleChinese) ? titleChinese : titleEnglish ?? "";

            /// <summary>The short title, or empty (the caller falls back to <see cref="Title"/>).</summary>
            public string ShortTitle(UiLanguage l) =>
                l == UiLanguage.Chinese ? (!string.IsNullOrEmpty(shortChinese) ? shortChinese : !string.IsNullOrEmpty(titleChinese) ? "" : shortEnglish ?? "")
                                        : shortEnglish ?? "";

            public Texture2D Image(UiLanguage l) => l == UiLanguage.Chinese && imageChinese != null ? imageChinese : imageEnglish;

            public string Caption(UiLanguage l) =>
                l == UiLanguage.Chinese && !string.IsNullOrEmpty(captionChinese) ? captionChinese : captionEnglish ?? "";
        }

        public List<Slide> slides = new List<Slide>();

        public int Count => slides != null ? slides.Count : 0;

        public static int CountOf(SlideSet s) => s != null ? s.Count : 0;
    }

    /// <summary>
    /// One page of <see cref="ProtocolScreens.Slides"/> at run time: from a <see cref="SlideSet.Slide"/>, or built in code for a
    /// stock slide (the recenter slide). <see cref="Ready"/> false keeps Next inactive (null = always ready).
    /// </summary>
    public sealed class SlidePage
    {
        public string Id = "slide";
        public Func<UiLanguage, string> Title;
        /// <summary>Label of the buttons that lead here; empty or null = <see cref="Title"/>.</summary>
        public Func<UiLanguage, string> ShortTitle;
        public Func<UiLanguage, Texture2D> Image;
        /// <summary>Text on the gray image area when there is no image.</summary>
        public Func<UiLanguage, string> Caption;
        public Func<bool> Ready;

        public static SlidePage From(SlideSet.Slide s) =>
            new SlidePage { Id = s.id, Title = s.Title, ShortTitle = s.ShortTitle, Image = s.Image, Caption = s.Caption };

        public static List<SlidePage> From(SlideSet set)
        {
            var list = new List<SlidePage>();
            if (set != null && set.slides != null)
                foreach (var s in set.slides)
                    if (s != null) list.Add(From(s));
            return list;
        }
    }
}
