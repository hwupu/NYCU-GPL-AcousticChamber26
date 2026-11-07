using System;
using System.IO;

namespace GPL.Acoustics
{
    /// <summary>
    /// RMS level (dBFS, over all channels and samples) of a WAV file read from disk, without loading it into Unity: PCM 16, 24, 32
    /// bit and IEEE float 32, plain or WAVE_FORMAT_EXTENSIBLE. Gives the same value as <c>ClipLevels.RmsDbfs</c> on the imported clip,
    /// so long clips can be imported as Streaming (GetData does not work on them) and their level measured in the Editor.
    /// </summary>
    public static class WavLevel
    {
        public struct Info
        {
            public int Channels, SampleRate, BitsPerSample;
            public bool Float;
            public long Frames;
            public double DurationS => SampleRate > 0 ? (double)Frames / SampleRate : 0;
        }

        public static float RmsDbfs(string path, out Info info)
        {
            using (var s = File.OpenRead(path)) return RmsDbfs(s, out info);
        }

        public static float RmsDbfs(Stream stream, out Info info)
        {
            info = default;
            var r = new BinaryReader(stream);
            if (Tag(r) != "RIFF") throw new InvalidDataException("not a RIFF file");
            r.ReadUInt32();
            if (Tag(r) != "WAVE") throw new InvalidDataException("not a WAVE file");

            int format = 0;
            bool haveFmt = false;
            while (stream.Position + 8 <= stream.Length)
            {
                string id = Tag(r);
                long size = r.ReadUInt32();
                long next = stream.Position + size + (size & 1);
                if (id == "fmt ")
                {
                    format = r.ReadUInt16();
                    info.Channels = r.ReadUInt16();
                    info.SampleRate = (int)r.ReadUInt32();
                    r.ReadUInt32();                         // byte rate
                    r.ReadUInt16();                         // block align
                    info.BitsPerSample = r.ReadUInt16();
                    if (format == 0xFFFE && size >= 40)
                    {
                        r.ReadUInt16(); r.ReadUInt16(); r.ReadUInt32(); // cbSize, valid bits, channel mask
                        format = r.ReadUInt16();            // first two bytes of the sub-format GUID = the format code
                    }
                    info.Float = format == 3;
                    if (format != 1 && format != 3) throw new InvalidDataException("unsupported WAV format " + format);
                    haveFmt = true;
                }
                else if (id == "data")
                {
                    if (!haveFmt) throw new InvalidDataException("data before fmt");
                    return Rms(r, size, ref info);
                }
                stream.Position = Math.Min(next, stream.Length);
            }
            throw new InvalidDataException("no data chunk");
        }

        static float Rms(BinaryReader r, long size, ref Info info)
        {
            int bytes = info.BitsPerSample / 8;
            if (bytes < 2 || bytes > 4 || (info.Float && bytes != 4)) throw new InvalidDataException("unsupported bits " + info.BitsPerSample);
            long samples = size / bytes;
            info.Frames = info.Channels > 0 ? samples / info.Channels : 0;
            double sum = 0;
            var buf = new byte[bytes * 65536];
            long left = samples * bytes;
            long count = 0;
            while (left > 0)
            {
                int n = r.Read(buf, 0, (int)Math.Min(buf.Length, left));
                if (n <= 0) break;
                left -= n;
                for (int i = 0; i + bytes <= n; i += bytes)
                {
                    double v;
                    if (info.Float) v = BitConverter.ToSingle(buf, i);
                    else if (bytes == 2) v = (short)(buf[i] | buf[i + 1] << 8) / 32768.0;
                    else if (bytes == 3) v = ((buf[i] << 8 | buf[i + 1] << 16 | buf[i + 2] << 24) >> 8) / 8388608.0;
                    else v = (buf[i] | buf[i + 1] << 8 | buf[i + 2] << 16 | buf[i + 3] << 24) / 2147483648.0;
                    sum += v * v;
                    count++;
                }
            }
            if (count == 0) return AudioLevelMath.Silent;
            double rms = Math.Sqrt(sum / count);
            return rms <= 1e-10 ? AudioLevelMath.Silent : (float)(20.0 * Math.Log10(rms));
        }

        static string Tag(BinaryReader r)
        {
            var b = r.ReadBytes(4);
            if (b.Length < 4) throw new InvalidDataException("truncated");
            return new string(new[] { (char)b[0], (char)b[1], (char)b[2], (char)b[3] });
        }
    }
}
