using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kiseki_PKG_Unpack
{
    class Program
    {
        static string Input_Path; // Path to directory to be compressed or pkg to be decompressed.
        static byte[] Input_File; // Compressed .pkg archive.

        // Unpacking
        static string PKG_Directory; // Directory where contents will be extracted;

        // Shared
        static List<File_Entry> Files_In_PKG = new List<File_Entry>(30); // List of file entries;
        static int Number_Of_Files; // Number of files in PKG archive;

        // Packing
        static List<byte> PKG_File = new List<byte>(10000);

        static void Main(string[] Arguments)
        {
            // Check Arguments
            try { Input_Path = Arguments[0]; } catch { Write_Help(); }

            // If it's a file then decompress, else it's a directory.
            if (File.Exists(Input_Path)) { Input_File = File.ReadAllBytes(Input_Path); Unarchive(); }
            else if (Directory.Exists(Input_Path)) { Archive(); }
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
            Number_Of_Files = BitConverter.ToInt32(Input_File, 4); // 0x4: File Count
            Directory.CreateDirectory(Path.ChangeExtension(Input_Path, null)); // Create Directory
            PKG_Directory = Path.ChangeExtension(Input_Path, null); // Set Directory Path

            // Get unknown file header.
            byte[] File_Start_Header = new byte[4]; Array.Copy(Input_File, File_Start_Header, 4);
            File.WriteAllBytes(PKG_Directory + "\\Start_Header.bin", File_Start_Header );

            // Pointer of where to read file in array.
            int File_Pointer = 8; // Starts at first file entry.

            // For each file, get properties of the file.
            for (int x = 0; x < Number_Of_Files; x++)
            {
                File_Entry PKG_Archive_File = new File_Entry();

                PKG_Archive_File.File_Name = Encoding.ASCII.GetString(Input_File, File_Pointer, File_Name_Length).TrimEnd('\0'); ; File_Pointer += File_Name_Length; // Get File Name
                PKG_Archive_File.File_Size = BitConverter.ToInt32(Input_File, File_Pointer); File_Pointer += 0x4; // Get File Size
                PKG_Archive_File.File_Compressed_Size = BitConverter.ToInt32(Input_File, File_Pointer); File_Pointer += 0x4; // Get Compressed Size
                PKG_Archive_File.File_Offset = BitConverter.ToInt32(Input_File, File_Pointer); File_Pointer += 0x4; // Get File Offset
                PKG_Archive_File.File_Compressed_Flag = BitConverter.ToInt32(Input_File, File_Pointer); File_Pointer += 0x4; // Get File Type

                Console.WriteLine("\nFile Name: " + PKG_Archive_File.File_Name);
                Console.WriteLine("File Size: " + PKG_Archive_File.File_Size);
                Console.WriteLine("Compressed Size: " + PKG_Archive_File.File_Compressed_Size);
                Console.WriteLine("File Offset: " + PKG_Archive_File.File_Offset);
                Console.WriteLine("Compressed Flag: " + PKG_Archive_File.File_Compressed_Flag);

                Files_In_PKG.Add(PKG_Archive_File);
            }

            // Extract each file
            for (int x = 0; x < Files_In_PKG.Count; x++)
            {
                // Linq statement, gets bytes of each file from the offset and length.
                byte[] File_Bytes = Input_File.Skip(Files_In_PKG[x].File_Offset).Take(Files_In_PKG[x].File_Compressed_Size).ToArray();

                // If file is compressed, decompress and write.
                if (Files_In_PKG[x].File_Compressed_Flag >= 1) { File.WriteAllBytes(PKG_Directory + "\\" + Files_In_PKG[x].File_Name, Decompress_File(File_Bytes)); }
                else { File.WriteAllBytes(PKG_Directory + "\\" + Files_In_PKG[x].File_Name, File_Bytes); }  
            }
        }

        /// Constants for Decompression
        const int File_Name_Length = 0x40;

        // Represents an individual file entry in the .pkg file header.
        struct File_Entry
        {
            public string File_Name;
            public int File_Size;
            public int File_Compressed_Size;
            public int File_Offset;
            public int File_Compressed_Flag; // Almost always 0x1
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
        private static byte[] Decompress_File(byte[] Compressed_File)
        {
            // Get file compression properties from header.
            uint Uncompressed_File_Size = BitConverter.ToUInt32(Compressed_File, 0); // Expected file size.
            uint Compressed_File_Size = BitConverter.ToUInt32(Compressed_File, 4); // Size of compressed data.
            uint Compression_Block_Key = BitConverter.ToUInt32(Compressed_File, 8); // Unique byte to represent start of compressed data.

            // Decompressed file will be written here.
            byte[] Decompressed_File = new byte[Uncompressed_File_Size];

            // Copy Raw Data to New Array
            byte[] Raw_Data = new byte[Compressed_File.Length - 12];
            Array.Copy((Array)Compressed_File, 12, (Array)Raw_Data, 0, Raw_Data.Length);

            // Decompression Time!
            int Decompressed_File_Destination_Index = 0; // Current pointer of where data is to be written onto the decompressed file.
            for (int Byte_Index = 0; Byte_Index < Raw_Data.Length; Byte_Index++)
            {
                byte Current_Byte = Raw_Data[Byte_Index]; // Get current byte.

                // If the byte matches the unique compression key.
                if ((int)Current_Byte == (int)Compression_Block_Key)
                {
                    Byte_Index++; byte Second_Byte = Raw_Data[Byte_Index]; // Obtain the next byte.

                    // If the 2nd byte matches unique compression key, copy data. (Safeguard in case the byte after compression key matches compression key).
                    if ((int)Second_Byte == (int)Compression_Block_Key)
                    {
                        Decompressed_File[Decompressed_File_Destination_Index] = Second_Byte;
                        Decompressed_File_Destination_Index++;
                    }
                    else // Decompression.
                    {
                        Byte_Index++; byte Third_Byte = Raw_Data[Byte_Index]; // Obtain Third Byte
                        if ((uint)Second_Byte >= Compression_Block_Key) { Second_Byte--; } // Generally happens when Block_Key = 0x00
                        // Write repeated bytes onto the file.
                        Array.Copy((Array)Decompressed_File, Decompressed_File_Destination_Index - (int)Second_Byte, (Array)Decompressed_File, Decompressed_File_Destination_Index, (int)Third_Byte);
                        Decompressed_File_Destination_Index += (int)Third_Byte; // Increase current index by amount of data written.
                    }
                }
                // If the byte is not the compression key, keep copying.
                else
                {
                    Decompressed_File[Decompressed_File_Destination_Index] = Current_Byte;
                    Decompressed_File_Destination_Index++;
                }
            }
            return Decompressed_File;
        }

        /////////////////////////////////
        /////////////////////////////////
        /////////////////////////////////
        static void Archive()
        {
            // Write Header & Remove File.
            PKG_File.AddRange(File.ReadAllBytes(Input_Path + "\\Start_Header.bin"));

            // Calculate where every file goes.
            DirectoryInfo Directory_Info = new DirectoryInfo(Input_Path + "\\");
            FileInfo[] PKG_Files = Directory_Info.GetFiles().Where(file => ((file.Name != "Start_Header.bin") && (file.Name != Input_Path.Substring(Input_Path.LastIndexOf(@"\") + 1) + ".pkg")) ).ToArray();

            // Write Number of Files
            Number_Of_Files = PKG_Files.Length;
            PKG_File.AddRange(BitConverter.GetBytes(Number_Of_Files));

            // Get properties for each file such as offset, compressed size etc.
            int File_Pointer = 0x8; // After header + number of files.
            int First_Data_Entry_Offset = (File_Entry_Length * Number_Of_Files) + 0x8;
            for (int x = 0; x < PKG_Files.Length; x++)
            {
                File_Entry PKG_File_Properties = new File_Entry();
                PKG_File_Properties.File_Name = PKG_Files[x].Name;
                PKG_File_Properties.File_Size = (int)PKG_Files[x].Length;
                PKG_File_Properties.File_Compressed_Size = (int)PKG_Files[x].Length;
                PKG_File_Properties.File_Compressed_Flag = 0;

                if (x == 0) { PKG_File_Properties.File_Offset = First_Data_Entry_Offset; }
                else { PKG_File_Properties.File_Offset = Files_In_PKG[x - 1].File_Offset + Files_In_PKG[x - 1].File_Size; }

                Files_In_PKG.Add(PKG_File_Properties);
            }

            // Write the file data.
            foreach (File_Entry PKG_File_Properties in Files_In_PKG)
            {
                // Get file name to write in correct format
                byte[] File_Name_Bytes = Encoding.ASCII.GetBytes(PKG_File_Properties.File_Name);

                // Initialize name array as 0x00
                byte[] File_Entry_Name_Proper = new byte[64]; for (int x = 0; x < File_Entry_Name_Proper.Length; x++) { File_Entry_Name_Proper[x] = 0x00; }

                // Copy name array
                Array.Copy(File_Name_Bytes, File_Entry_Name_Proper, File_Name_Bytes.Length);

                PKG_File.AddRange(File_Entry_Name_Proper); // File name
                PKG_File.AddRange(BitConverter.GetBytes(PKG_File_Properties.File_Size)); // File size
                PKG_File.AddRange(BitConverter.GetBytes(PKG_File_Properties.File_Compressed_Size)); // File compressed size
                PKG_File.AddRange(BitConverter.GetBytes(PKG_File_Properties.File_Offset)); // File offset
                PKG_File.AddRange(BitConverter.GetBytes(PKG_File_Properties.File_Compressed_Flag)); // File compressed flag
            }

            // Write out each file
            foreach (File_Entry PKG_File_Properties in Files_In_PKG)
            {
                PKG_File.AddRange(File.ReadAllBytes(Input_Path + "\\" + PKG_File_Properties.File_Name));
            }

            // Write the file.
            File.WriteAllBytes(Input_Path + "\\" + Input_Path.Substring(Input_Path.LastIndexOf(@"\") + 1) + ".pkg", PKG_File.ToArray());
        }

        /// Constants for Decompression
        const int File_Entry_Length = 0x50;
    }
}
