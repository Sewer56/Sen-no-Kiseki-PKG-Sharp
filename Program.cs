using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kiseki_PKG_Unpack
{
    class Program
    {
        static string _inputPath; // Path to directory to be compressed or pkg to be decompressed.
        static byte[] _inputFile; // Compressed .pkg archive.

        // Unpacking
        static string _pkgDirectory; // Directory where contents will be extracted;

        // Shared
        static List<FileEntry> _filesInPkg = new List<FileEntry>(30); // List of file entries;
        static int _numberOfFiles; // Number of files in PKG archive;

        // Packing
        static List<byte> _pkgFile = new List<byte>(10000);

        static void Main(string[] arguments)
        {
            // Check Arguments
            try { _inputPath = arguments[0]; } catch { Write_Help(); }

            // If it's a file then decompress, else it's a directory.
            if (File.Exists(_inputPath)) { _inputFile = File.ReadAllBytes(_inputPath); Unarchive(); }
            else if (Directory.Exists(_inputPath)) { Archive(); }
        }

        static void Write_Help()
        {
            Console.WriteLine("Kiseki PKG Tool by Sewer56lol\n" +
                "Unarchiver, Rearchiver & Decompressor for PC + PS3 Sen no Kiseki/Trails of Cold Steel PKG files.\n" +
                "Usage: `Kiseki_PKG_Tool.exe <Directory>` or `Kiseki_PKG_Tool.exe <PKGFile>`\n" +
                "Output is directory of source PKG or inside specified directory.");
            Console.ReadLine();
        }

        /////////////////////////////////
        /////////////////////////////////
        /////////////////////////////////

        /// <summary>
        /// Decompression Time!
        /// </summary>
        static void Unarchive()
        {
            // Get number of files and directory to output to.
            _numberOfFiles = BitConverter.ToInt32(_inputFile, 4); // 0x4: File Count
            Directory.CreateDirectory(Path.ChangeExtension(_inputPath, null)); // Create Directory
            _pkgDirectory = Path.ChangeExtension(_inputPath, null); // Set Directory Path

            // Get unknown file header.
            byte[] fileStartHeader = new byte[4];
            Array.Copy(_inputFile, fileStartHeader, 4);
            File.WriteAllBytes(_pkgDirectory + "\\Start_Header.bin", fileStartHeader );

            // Pointer of where to read file in array.
            int filePointer = 8; // Starts at first file entry.

            // For each file, get properties of the file.
            for (int x = 0; x < _numberOfFiles; x++)
            {
                FileEntry pkgArchiveFile = new FileEntry();

                pkgArchiveFile.FileName = Encoding.ASCII.GetString(_inputFile, filePointer, FileNameLength).TrimEnd('\0'); ; filePointer += FileNameLength; // Get File Name
                pkgArchiveFile.FileSize = BitConverter.ToInt32(_inputFile, filePointer); filePointer += 0x4; // Get File Size
                pkgArchiveFile.FileCompressedSize = BitConverter.ToInt32(_inputFile, filePointer); filePointer += 0x4; // Get Compressed Size
                pkgArchiveFile.FileOffset = BitConverter.ToInt32(_inputFile, filePointer); filePointer += 0x4; // Get File Offset
                pkgArchiveFile.FileCompressedFlag = BitConverter.ToInt32(_inputFile, filePointer); filePointer += 0x4; // Get File Type

                Console.WriteLine("\nFile Name: " + pkgArchiveFile.FileName);
                Console.WriteLine("File Size: " + pkgArchiveFile.FileSize);
                Console.WriteLine("Compressed Size: " + pkgArchiveFile.FileCompressedSize);
                Console.WriteLine("File Offset: " + pkgArchiveFile.FileOffset);
                Console.WriteLine("Compressed Flag: " + pkgArchiveFile.FileCompressedFlag);

                _filesInPkg.Add(pkgArchiveFile);
            }

            // Extract each file
            for (int x = 0; x < _filesInPkg.Count; x++)
            {
                // Linq statement, gets bytes of each file from the offset and length.
                byte[] fileBytes = _inputFile.Skip(_filesInPkg[x].FileOffset).Take(_filesInPkg[x].FileCompressedSize).ToArray();

                // If file is compressed, decompress and write.
                if (_filesInPkg[x].FileCompressedFlag >= 1) { File.WriteAllBytes(_pkgDirectory + "\\" + _filesInPkg[x].FileName, Decompress_File(fileBytes)); }
                else { File.WriteAllBytes(_pkgDirectory + "\\" + _filesInPkg[x].FileName, fileBytes); }  
            }
        }

        /// Constants for Decompression
        const int FileNameLength = 0x40;

        // Represents an individual file entry in the .pkg file header.
        struct FileEntry
        {
            public string FileName;
            public int FileSize;
            public int FileCompressedSize;
            public int FileOffset;
            public int FileCompressedFlag; // Almost always 0x1
        }

        /*
        Sen no Kiseki's RLE3 based compression in a nutshell.

        File Header:
        [code]
        int32 Uncompressed_File_Length
        int32 Compressed_File_Length
        int32 Compression_Key // Defines a compression block.
        [/code]

        (Everything after is just data)

        Compression Block:
        [code]
        byte Compression_Key
        byte Previous_Bytes_Offset
        byte Length_Of_Data
        [/code]

        Decompression works simply by reusing data previously present in the file, when the game encounters a byte equal to the `Compression_Key`, 
        it reads an offset to a place previously in the file and the length of data which is repeated. 
        The data of that length from that defined offset goes where the compression key/block was first found in its place. 
        If the offset is greater than the compression_key, it is decremented by one.

        Special exception: If the compression key is repeated twice, just write it once like normal during decompression.
        Basically read the 0x12 byte header, then copy following raw data byte by byte into a new array which will represent a file. 
        If byte equal to compression key is met, do appropriate stuff.
        */

        // Compressed: Compressed File >> Same as our output.
        private static byte[] Decompress_File(byte[] compressedFile)
        {
            // Get file compression properties from header.
            uint uncompressedFileSize = BitConverter.ToUInt32(compressedFile, 0); // Expected file size.
            uint compressedFileSize = BitConverter.ToUInt32(compressedFile, 4); // Size of compressed data.
            uint compressionBlockKey = BitConverter.ToUInt32(compressedFile, 8); // Unique byte to represent start of compressed data.

            // Decompressed file will be written here.
            byte[] decompressedFile = new byte[uncompressedFileSize];

            // Copy Raw Data to New Array
            byte[] rawData = new byte[compressedFile.Length - 12];
            Array.Copy((Array)compressedFile, 12, (Array)rawData, 0, rawData.Length);

            // Decompression Time!
            int decompressedFileIndex = 0; // Current pointer of where data is to be written onto the decompressed file.
            for (int byteIndex = 0; byteIndex < rawData.Length; byteIndex++)
            {
                byte currentByte = rawData[byteIndex]; // Get current byte.

                // If the byte matches the unique compression key.
                if ((int)currentByte == (int)compressionBlockKey)
                {
                    byteIndex++;
                    byte secondByte = rawData[byteIndex]; // Obtain the next byte.

                    // If the 2nd byte matches unique compression key, copy data. (Safeguard in case the byte after compression key matches compression key).
                    if ((int)secondByte == (int)compressionBlockKey)
                    {
                        decompressedFile[decompressedFileIndex] = secondByte;
                        decompressedFileIndex++;
                    }
                    else // Decompression.
                    {
                        byteIndex++;
                        byte thirdByte = rawData[byteIndex]; // Obtain Third Byte
                        if ((uint)secondByte > compressionBlockKey) { secondByte--; }  // Generally happens when Block_Key = 0x00
                                                                                        // Write repeated bytes onto the file.
                        Array.Copy((Array)decompressedFile, decompressedFileIndex - (int)secondByte, (Array)decompressedFile, decompressedFileIndex, (int)thirdByte);
                        decompressedFileIndex += (int)thirdByte; // Increase current index by amount of data written.
                    }
                }
                // If the byte is not the compression key, keep copying.
                else
                {
                    decompressedFile[decompressedFileIndex] = currentByte;
                    decompressedFileIndex++;
                }
            }
            return decompressedFile;
        }

        /////////////////////////////////
        /////////////////////////////////
        /////////////////////////////////
        static void Archive()
        {
            // Write Header & Remove File.
            _pkgFile.AddRange(File.ReadAllBytes(_inputPath + "\\Start_Header.bin"));

            // Calculate where every file goes.
            DirectoryInfo directoryInfo = new DirectoryInfo(_inputPath + "\\");
            FileInfo[] pkgFiles = directoryInfo.GetFiles().Where(file => ((file.Name != "Start_Header.bin") && (file.Name != _inputPath.Substring(_inputPath.LastIndexOf(@"\") + 1) + ".pkg")) ).ToArray();

            // Write Number of Files
            _numberOfFiles = pkgFiles.Length;
            _pkgFile.AddRange(BitConverter.GetBytes(_numberOfFiles));

            // Get properties for each file such as offset, compressed size etc.
            int filePointer = 0x8; // After header + number of files.
            int firstDataEntryOffset = (FileEntryLength * _numberOfFiles) + 0x8;
            for (int x = 0; x < pkgFiles.Length; x++)
            {
                FileEntry pkgFileProperties = new FileEntry();
                pkgFileProperties.FileName = pkgFiles[x].Name;
                pkgFileProperties.FileSize = (int)pkgFiles[x].Length;
                pkgFileProperties.FileCompressedSize = (int)pkgFiles[x].Length;
                pkgFileProperties.FileCompressedFlag = 0;

                if (x == 0) { pkgFileProperties.FileOffset = firstDataEntryOffset; }
                else { pkgFileProperties.FileOffset = _filesInPkg[x - 1].FileOffset + _filesInPkg[x - 1].FileSize; }

                _filesInPkg.Add(pkgFileProperties);
            }

            // Write the file data.
            foreach (FileEntry pkgFileProperties in _filesInPkg)
            {
                // Get file name to write in correct format
                byte[] fileNameBytes = Encoding.ASCII.GetBytes(pkgFileProperties.FileName);

                // Initialize name array as 0x00
                byte[] fileEntryNameProper = new byte[64]; for (int x = 0; x < fileEntryNameProper.Length; x++) { fileEntryNameProper[x] = 0x00; }

                // Copy name array
                Array.Copy(fileNameBytes, fileEntryNameProper, fileNameBytes.Length);

                _pkgFile.AddRange(fileEntryNameProper); // File name
                _pkgFile.AddRange(BitConverter.GetBytes(pkgFileProperties.FileSize)); // File size
                _pkgFile.AddRange(BitConverter.GetBytes(pkgFileProperties.FileCompressedSize)); // File compressed size
                _pkgFile.AddRange(BitConverter.GetBytes(pkgFileProperties.FileOffset)); // File offset
                _pkgFile.AddRange(BitConverter.GetBytes(pkgFileProperties.FileCompressedFlag)); // File compressed flag
            }

            // Write out each file
            foreach (FileEntry pkgFileProperties in _filesInPkg)
            {
                _pkgFile.AddRange(File.ReadAllBytes(_inputPath + "\\" + pkgFileProperties.FileName));
            }

            // Write the file.
            File.WriteAllBytes(_inputPath + "\\" + _inputPath.Substring(_inputPath.LastIndexOf(@"\") + 1) + ".pkg", _pkgFile.ToArray());
        }

        /// Constants for Decompression
        const int FileEntryLength = 0x50;
    }
}
