using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PKGLib.Definitions.Native;
using PKGLib.Utilities;
using SenCompressSharp;

namespace PKGLib.Definitions.Managed
{
    public class File
    {
        /// <summary>
        /// Name of file (ANSI)
        /// </summary>
        public string           FileName        { get; set; }

        /// <summary>
        /// Size of file before compression.
        /// </summary>
        public int              FileSize        { get; private set; }

        /// <summary>
        /// Size of file after compression (including 12 byte header if CompressionType RLE).
        /// </summary>
        public int              CompressedSize  { get; private set; }

        /// <summary>
        /// Specifies the type of compression used.
        /// </summary>
        public CompressionType  CompressedFlag  { get; private set; }

        private Memory<byte>    _data;

        /// <summary>
        /// Stores the raw bytes of the file.
        /// The getter returns uncompresses the file automatically if it is compressed.
        /// To get the internally stored data (which may be compressed) use <see cref="RawData"/>
        /// To set new data for the file use SetFileData();
        /// </summary>
        public byte[]           Data
        {
            get             => GetUncompressedFile();
            private set     => _data = value;
        }

        public byte[]          RawData => _data.ToArray();

        /// <summary>
        /// Creates a new file from a file path.
        /// </summary>
        /// <param name="filePath">The path of the individual file.</param>
        /// <param name="compress">Set to true for the internal file to be compressed.</param>
        public File(string filePath, bool compress)
        {
            byte[] data = System.IO.File.ReadAllBytes(filePath);

            FileName = Path.GetFileName(filePath);
            SetFileData(data, compress);
        }

        /// <summary>
        /// Creates a new file from an already existing array of bytes containing an uncompressed file.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="rawData">The bytes to create the file from.</param>
        /// <param name="compress">Set to true for the individual file to be compressed.</param>
        public File(string fileName, byte[] rawData, bool compress)
        {
            FileName = fileName;
            SetFileData(rawData, compress);
        }

        /// <summary>
        /// Creates a new managed file from an existing native file struct read directly from the PKG file.
        /// </summary>
        /// <param name="entry">The file entry to base the file from.</param>
        /// <param name="pkgFile">The bytes of the actual PKG file that this file entry originated from.</param>
        public unsafe File(ref FileEntry entry, byte[] pkgFile)
        {
            fixed (byte* fileName = entry.FileName)
            {
                FileName = StringUtilities.CharPointerToString(fileName);
            }

            FileSize            = entry.FileSize;
            CompressedSize      = entry.CompressedSize;
            CompressedFlag      = entry.CompressedFlag;
            _data               = new Memory<byte>(pkgFile, entry.FileOffset, entry.CompressedSize);
        }

        /// <summary>
        /// Sets new file data for this file.
        /// </summary>
        /// <param name="data">The uncompressed file data to set for this file.</param>
        /// <param name="compress">Set to true to compress the file.</param>
        /// <param name="searchBufferSize">(Default = 254)
        /// A value preferably between 0 and 254 that declares how many bytes
        /// the compressor visit before any specific byte to search for matching patterns.
        /// Increasing this value compresses the data to smaller filesizes at the expense of compression time.
        /// Changing this value has no noticeable effect on decompression time.</param>
        public void SetFileData(byte[] data, bool compress, int searchBufferSize = 254)
        {
            FileSize            = data.Length;

            if (compress)
            {
                _data           = SenCompress.Compress(data, searchBufferSize);
                CompressedFlag  = CompressionType.CompressionRLE;
            }
            else
            {
                _data           = data;
                CompressedFlag  = CompressionType.NoCompression;
            }

            
            CompressedSize      = _data.Length;
        }

        /// <summary>
        /// Decompresses the file present in the <see cref="Data"/> 
        /// </summary>
        /// <returns></returns>
        public unsafe byte[] GetUncompressedFile()
        {
            if (CompressedFlag == CompressionType.NoCompression)
                return _data.ToArray();
            else if (CompressedFlag == CompressionType.CompressionRLE)
                fixed (byte* uncompressedPtr = _data.Span)
                    return SenCompress.Decompress(uncompressedPtr, _data.Length);
            else if (CompressedFlag == CompressionType.CompressionUnknown)
                throw new NotSupportedException("PKG Compression Type 2 not supported");
            else
                throw new NotSupportedException("Unknown Compression Format");
        }
    }
}
