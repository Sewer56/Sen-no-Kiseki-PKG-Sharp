using System.Runtime.InteropServices;
using PKGLib.Definitions.Managed;
using PKGLib.Utilities;

namespace PKGLib.Definitions.Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe struct FileEntry
    {
        public fixed byte       FileName[64];           // Name of file (ANSI)
        public int              FileSize;           // Size of file before compression.
        public int              CompressedSize;     // Size of file after compression (including 12 byte header).
        public int              FileOffset;         // Offset to File struct.
        public CompressionType  CompressedFlag;     // 0 = Uncompressed; 1 = Compressed

        /// <summary>
        /// Generates a new native <see cref="FileEntry"/> from an instance of the <see cref="File"/> class.
        /// </summary>
        public static FileEntry FromFile(File file)
        {
            FileEntry entry         = new FileEntry();
            entry.CompressedSize    = file.CompressedSize;
            entry.CompressedFlag    = file.CompressedFlag;
            StringUtilities.StringToCharPointer(file.FileName, entry.FileName);
            entry.FileSize          = file.FileSize;

            return entry;
        }
    };
}
