﻿using System;
using System.IO;
using FFmpeg.AutoGen;

namespace OpenVIII.AV
{
    /// <summary>
    /// Used only when reading ADPCM data from memory.
    /// </summary>
    public struct BufferData
    {
        public enum TargetFile
        {
            SoundDat,
            OtherZzz
        }

        #region Fields

        public long DataSeekLoc { get; set; }
        private long _totalReadData;
        public long DataSize { get; set; }
        public uint HeaderSize { get; set; }
        private IntPtr _header;
        public TargetFile Target { get; set; }

        #endregion Fields

        #region Properties

        public static string DataFileName { get; private set; }

        #endregion Properties

        #region Methods
        
        public unsafe int Read(byte* buf, int bufSize)
        {   
            int ret;
            if (HeaderSize >0 && (ret = ReadHeader(buf, bufSize)) != ffmpeg.AVERROR_EOF)
                return ret;
            return ReadData(buf, bufSize);
        }

        public unsafe void SetHeader(byte* value) => _header = (IntPtr)value;

        private unsafe int ReadData(byte* buf, int bufSize)
        {
            if (string.IsNullOrWhiteSpace(DataFileName))
                DataFileName = Path.Combine(Memory.FF8DIRdata, "Sound", "audio.dat");

            bufSize = Math.Min(bufSize, (int)DataSize);

            if (bufSize == 0)
            {
                return ffmpeg.AVERROR_EOF;
            }
            Stream s;
            ArchiveZzz other;
            switch (Target)
            {
                case TargetFile.SoundDat:
                    if (File.Exists(DataFileName))
                    {
                        s = new FileStream(DataFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    }
                    else
                    {
                        other = (ArchiveZzz)ArchiveZzz.Load(Memory.Archives.ZZZ_OTHER);
                        s = new MemoryStream(other.GetBinaryFile("audio.dat", true), false);
                    }
                    break;
                case TargetFile.OtherZzz:
                    other = (ArchiveZzz)ArchiveZzz.Load(Memory.Archives.ZZZ_OTHER);
                    s = other.OpenStream();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(BufferData)}::{nameof(ReadData)}::{nameof(Target)} Value out of range! Target = {{{Target}}}");
            }

            // binaryReader disposes of fs
            if(s == null) throw new NullReferenceException($"{nameof(BufferData)}::{nameof(ReadData)} stream is null");
            using (BinaryReader br = new BinaryReader(s))
            {
                s.Seek(DataSeekLoc, SeekOrigin.Begin);
                using (UnmanagedMemoryStream ums = new UnmanagedMemoryStream(buf, bufSize, bufSize, FileAccess.Write))
                {
                    // copy public buffer data to buf
                    ums.Write(br.ReadBytes(bufSize), 0, bufSize);
                    DataSeekLoc += bufSize;
                    _totalReadData += bufSize;
                    DataSize -= bufSize;

                    return bufSize;
                }
            }
        }

        private unsafe int ReadHeader(byte* buf, int bufSize)
        {
            bufSize = Math.Min(bufSize, (int)HeaderSize);

            if (bufSize == 0)
            {
                return ffmpeg.AVERROR_EOF;
            }

            // copy public buffer data to buf
            Buffer.MemoryCopy((void*)_header, buf, bufSize, bufSize);
            _header += bufSize;
            HeaderSize -= (uint)bufSize;

            return bufSize;
        }

        internal long Seek(long offset, int whence)
        {
            switch (whence)
            {
                case ffmpeg.AVSEEK_SIZE:
                    return offset == 0
                        ? _totalReadData
                        : throw new Exception($"unknown {nameof(whence)}: {whence}, {nameof(offset)}: {offset}");

                case 0:
                    offset -= _totalReadData;
                    break;
                default:
                    throw new Exception($"unknown {nameof(whence)}: {whence}");
             }

            DataSeekLoc += offset;
            _totalReadData += offset;
            DataSize -= offset;
            return _totalReadData;
        }

        #endregion Methods
    }
}