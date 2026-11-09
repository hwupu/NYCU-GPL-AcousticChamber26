namespace GPL.Stimuli
{
    /// <summary>
    /// The OpenAIR anechoic recordings in <c>Library/OpenAIR</c> (Univ. of York AudioLab, openairlib.net), taken unchanged from the
    /// CC BY-SA part of the snapshot doi:10.5281/zenodo.1186905 (2018-02-26; CRC of every file checked). Credit: "www.openairlib.net"
    /// (the snapshot's README) and Murphy &amp; Shelley 2010. Share-alike: a changed copy (trimmed, normalized) stays CC BY-SA.
    /// </summary>
    public static class OpenAir
    {
        const string Source = "https://www.openairlib.net/anechoicdb (snapshot doi:10.5281/zenodo.1186905)";
        const string Licence = "CC-BY-SA-4.0";
        const string Credit = "Anechoic recording from the OpenAIR library, www.openairlib.net (Univ. of York AudioLab), CC BY-SA. " +
                              "Murphy, D. T., & Shelley, S. (2010). OpenAIR: An interactive auralization web resource and database. 129th AES Convention, paper 8226.";

        static LibraryEntry E(string id, string file, string title, string format) =>
            new LibraryEntry(id, "OpenAIR/" + file, title, "OpenAIR, Univ. of York (performer: see the item page)", Source, Licence, Credit,
                             format + "; unchanged from the snapshot");

        public static readonly LibraryEntry[] Entries =
        {
            E("openair_bicycle_classic", "Bicycle_Horn/classichorn.wav", "Bicycle horn, classic, single squeeze", "mono 44.1 kHz 16-bit, 0.58 s"),
            E("openair_bicycle_classic_double", "Bicycle_Horn/classichorn_double.wav", "Bicycle horn, classic, double squeeze", "mono 44.1 kHz 16-bit, 0.83 s"),
            E("openair_bicycle_choked", "Bicycle_Horn/chokedhorn.wav", "Bicycle horn, choked", "mono 44.1 kHz 16-bit, 1.52 s"),
            E("openair_bicycle_pap", "Bicycle_Horn/paphorn.wav", "Bicycle horn, pap", "mono 44.1 kHz 16-bit, 0.55 s"),
            E("openair_starter_pistol", "Starter_Pistol/anechoic_gunshot_0.wav", "Starter pistol shot (impulsive)", "mono 96 kHz 16-bit, 1.09 s"),
            E("openair_operatic_voice", "Operatic_Voice/singing.wav", "Operatic voice, singing", "mono 48 kHz 16-bit, 4.87 s"),
            E("openair_baroque_trumpet_fanfare", "Natural_Baroque_Trumpet/tr-1780d-fanfare.wav", "Natural baroque trumpet (1780), fanfare", "mono 96 kHz 16-bit, 14.72 s"),
        };
    }
}
