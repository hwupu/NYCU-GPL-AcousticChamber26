using System;

namespace AcousticChamber.App
{
    /// <summary>
    /// Geometry of the Main Hall card grid (DesignSystem section 7; Storybook Screens/Main Hall, PO 2026-11-10): two columns
    /// <see cref="Gap"/> apart, rows <see cref="RowGap"/> apart; a card is only its 16:9 cover (the C-ID and the title are on it).
    /// The gaps leave room for a hovered card to grow (103 %). Units are canvas units; positions are card centers (x) and card
    /// top edges below the content top.
    /// </summary>
    public readonly struct HallGrid
    {
        public const int Columns = 2;
        public const float CoverAspect = 16f / 9f;
        public const float DefaultGap = 24f, DefaultRowGap = 28f;

        public readonly int Count;
        public readonly float Gap, RowGap;
        public readonly float CardWidth, CardHeight;

        public HallGrid(float contentWidth, int count, float gap = DefaultGap, float rowGap = DefaultRowGap)
        {
            if (contentWidth <= 0f || float.IsNaN(contentWidth)) throw new ArgumentOutOfRangeException(nameof(contentWidth));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
            Gap = gap;
            RowGap = rowGap;
            CardWidth = (contentWidth - (Columns - 1) * gap) / Columns;
            CardHeight = CardWidth / CoverAspect;
        }

        /// <summary>The cover is the whole card.</summary>
        public float CoverWidth => CardWidth;
        public float CoverHeight => CardHeight;

        public int Rows => (Count + Columns - 1) / Columns;

        /// <summary>Height of all rows (0 without cards).</summary>
        public float TotalHeight => Rows == 0 ? 0f : Rows * CardHeight + (Rows - 1) * RowGap;

        /// <summary>Center x of card <paramref name="index"/> (0 = top left, row by row).</summary>
        public float X(int index) => (index % Columns == 0 ? -1f : 1f) * (CardWidth + Gap) / 2f;

        /// <summary>Distance of card <paramref name="index"/>'s top edge below the content top.</summary>
        public float Top(int index) => (index / Columns) * (CardHeight + RowGap);
    }
}
