using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using PKGLib.Definitions.Managed;
using PKGLib.Definitions.Native;
using static PKGLib.Utilities.PACLibrary.Utilities.StructUtilities;

namespace PKGLib
{
    public class Archive
    {
        /// <summary>
        /// Contains the list of files to be present in the PKG archive.
        /// </summary>
       public List<File> Files = new List<File>(128);

        /* Constructors */

        /// <summary>
        /// Generates a new instance of <see cref="Archive"/> from a file path pointing
        /// to a Sen no Kiseki PKG file.
        /// </summary>
        /// <param name="filePath">A path to a Sen no Kiseki PKG file on the disk.</param>
        public Archive(string filePath)
        {
            FromPKGData(System.IO.File.ReadAllBytes(filePath));
        }

        /// <summary>
        /// Generates a new instance of <see cref="Archive"/> from an array of bytes containing
        /// a Sen no Kiseki PKG file.
        /// </summary>
        /// <param name="data">A byte array containing a Sen no Kiseki PKG file.</param>
        public Archive(byte[] data)
        {
            FromPKGData(data);
        }

        /// <summary>
        /// Creates a new empty PKG archive.
        /// </summary>
        public  Archive() { }

        /* Constructor (Continued) */

        private void FromPKGData(byte[] sourceFile)
        {
            // Contains the file pointer incremented on each structure read.
            int sourcePointer = 0;

            // Get file count in file.
            Header fileHeader = ArrayToStructureUnsafe<Header>(ref sourceFile, sourcePointer, ref sourcePointer);

            // Parse each file entry and add new managed file based from it.
            for (int x = 0; x < fileHeader.FileCount; x++)
            {
                FileEntry entry = ArrayToStructure<FileEntry>(ref sourceFile, sourcePointer, ref sourcePointer);
                Files.Add(new File(ref entry, sourceFile));
            }
        }

        /* Import/Export */

        /// <summary>
        /// Generates a new PKG file from the current <see cref="Archive"/> and saves it
        /// to a specified location.
        /// </summary>
        /// <param name="filePath"></param>
        public void Save(string filePath)
        {
            System.IO.File.WriteAllBytes(filePath, GeneratePKG());
        }


        /// <summary>
        /// Generates a PKG file from the current <see cref="Archive"/>.
        /// </summary>
        /// <returns>An array of bytes containing the newly generated PKG file.</returns>
        public unsafe byte[] GeneratePKG()
        {
            // Create the bytes of the PKG.
            List<byte> pkgBytes = new List<byte>(GetFileSizeEstimate());

            // Write header.
            Header fileHeader = new Header { FileCount = Files.Count };
            pkgBytes.AddRange(ConvertStructureToByteArrayUnsafe(ref fileHeader));

            // Get and write file entries.
            FileEntry[] fileEntries = GetFileEntries();

            for (int x = 0; x < fileEntries.Length; x++)
                pkgBytes.AddRange(ConvertStructureToByteArrayUnsafe(ref fileEntries[x]));

            // Now append pure files.
            for (int x = 0; x < Files.Count; x++)
                pkgBytes.AddRange(Files[x].RawData);

            return pkgBytes.ToArray();
        }

        /// <summary>
        /// Creates an estimate of the final file size of the PKG before exporting.
        /// </summary>
        private unsafe int GetFileSizeEstimate()
        {
            // Build estimate of the total file size to be generated.
            int fileSizeEstimate = sizeof(Header);

            // Add file entries
            fileSizeEstimate += Marshal.SizeOf<FileEntry>() * Files.Count;

            // Add file data
            for (int x = 0; x < Files.Count; x++)
                fileSizeEstimate += Files[x].CompressedSize;

            return fileSizeEstimate;
        }

        /// <summary>
        /// Generates a new set of file entries complete with offsets for each file.
        /// </summary>
        /// <returns></returns>
        private unsafe FileEntry[] GetFileEntries()
        {
            // First populate the file entries.
            FileEntry[] fileEntries = new FileEntry[Files.Count];

            for (int x = 0; x < Files.Count; x++)
                fileEntries[x] = FileEntry.FromFile(Files[x]);

            // Now calculate offsets.
            int firstFileOffset = sizeof(Header) + (Marshal.SizeOf<FileEntry>() * Files.Count);

            for (int x = 0; x < Files.Count; x++)
            {
                if (x == 0)
                    fileEntries[x].FileOffset = firstFileOffset;
                else
                    fileEntries[x].FileOffset = fileEntries[x - 1].FileOffset + fileEntries[x - 1].CompressedSize;
            }

            return fileEntries;
        }
    }
}
