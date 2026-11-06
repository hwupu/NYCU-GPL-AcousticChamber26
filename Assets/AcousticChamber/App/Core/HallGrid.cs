using System;

namespace AcousticChamber.App
{
    /// <summary>
    /// Geometry of the Main Hall card grid (DesignSystem section 7): two columns <see cref="Gap"/> apart, rows
    /// <see cref="RowGap"/> apart; each card holds a 16:9 cover inset <see cref="Inset"/> from the card edge and a title
    /// band below it. Units are canvas units; positions are card centers (x) and card top edges below the content top.
    /// </summary>
    public readonly struct HallGrid
    {
        public const int Columns = 2;
        public const float CoverAspect = 16f / 9f;
        public const float DefaultTitleBand = 72f;

        public readonly int Count;
        public readonly float Inset, Gap, RowGap, TitleBand;
        public readonly float CardWidth, CardHeight, CoverWidth, CoverHeight;

        public HallGrid(float contentWidth, int count, float inset = 4f, float gap = 4f, float rowGap = 8f,
                        float titleBand = DefaultTitleBand)
        {
            if (contentWidth <= 0f || float.IsNaN(contentWidth)) throw new ArgumentOutOfRangeException(nameof(contentWidth));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
            Inset = inset;
            Gap = gap;
            RowGap = rowGap;
            TitleBand = titleBand;
            CardWidth = (contentWidth - (Columns - 1) * gap) / Columns;
            CoverWidth = CardWidth - 2f * inset;
            CoverHeight = CoverWidth / CoverAspect;
            CardHeight = inset + CoverHeight + titleBand;
        }

        public int Rows => (Count + Columns - 1) / Columns;

        /// <summary>Height of all rows (0 without cards).</summary>
        public float TotalHeight => Rows == 0 ? 0f : Rows * CardHeight + (Rows - 1) * RowGap;

        /// <summary>Center x of card <paramref name="index"/> (0 = top left, row by row).</summary>
        public float X(int index) => (index % Columns == 0 ? -1f : 1f) * (CardWidth + Gap) / 2f;

        /// <summary>Distance of card <paramref name="index"/>'s top edge below the content top.</summary>
        public float Top(int index) => (index / Columns) * (CardHeight + RowGap);

        /// <summary>Center y of the cover relative to the card center.</summary>
        public float CoverCenterY => CardHeight / 2f - Inset - CoverHeight / 2f;
    }
}
