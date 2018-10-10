# Table of Contents
- [Downloading the Libraries from NuGet](#downloading-the-libraries-from-nuget)
  - [Using the Compression Library (SenCompress)](#using-the-compression-library-sencompress)
  - [Using the PKG Library (PKGLib)](#using-the-pkg-library-pkglib)
    - [The File Class](#the-file-class)

## Downloading the Libraries from NuGet

Both the PKG and Compression archives can be found on NuGet and easily consumed from your .NET project using the NuGet Package Manager.

Inside Visual Studio in your own project; open the `TOOLS` dropdown and select the `NuGet Package Manager` => `Manage Packages for Solution option`. 

![Exhibit A](https://i.imgur.com/m1Ybg19.png)

From there; simply type the name of the PKG library (PKGLib) or the compression library (SenCompress) and install them to your current solution.

If this option is not available under Tools; visit the Visual Studio insatller and ensure that NuGet is installed under individual components.

### Using the Compression Library (SenCompress)

Using the compression library is fairly simple; the `SenCompress` static class exposes a simple interface which allows you to painlessly compress and decompress files.

```csharp
public static byte[] Compress(byte[] data, int searchBufferSize); // searchBufferSize is optional.
public static byte[] Decompress(byte[] data);
public static byte[] Decompress(byte* dataPtr, int length);
```

Thus using the library simply reduces to this:

```csharp
// At the start of C# class.
using SenCompressSharp;

// Inside some function.
byte[] file             = File.ReadAllBytes("Arianrhod.bmp");
byte[] compressed       = SenCompress.Compress(file);
byte[] decompressed     = SenCompress.Decompress(compressed);
```

### Using the PKG Library (PKGLib)

Using the PKG library is equally as simple; the library exposes one singular class targeted for the end user named `Archive`; which can be used to load, edit and save PKG archives.

```csharp
// Top of file
using PKGLib;

Archive archiveFromFile = new Archive("C_PLY000.pkg");  // From file.
Archive archiveFromByteArray = new Archive(byteArray);  // From byte array containing file.
Archive emptyArchive = new Archive();                   // Empty archive; if you want to make your own.
```

From there; the archive exposes a simple easy to use interface.

```csharp
public void Save(string filePath);  // Generates a PKG file and saves it to a location.
public byte[] GeneratePKG();        // Generates a PKG file and returns a byte array of the file without save.
```

And a simple List<T> of files under `Archive.Files` which you may edit to add/remove your heart's content.

```csharp
archive.Files.Add(new File("RennnesTeaParty.bmp", true));
```

#### The File Class

**Creating a new file**:
```public
public File(string filePath, bool compress);                // From a file in your hard drive.
public File(string fileName, byte[] rawData, bool compress);// From name and byte array.
public File(ref FileEntry entry, byte[] pkgFile);           // For low level/internal/dev use.
// Setting the compress bool to true will cause the file to be internally compressed.
```

**Getting decompressed data**:
```public
// Use this even if your file is already decompressed.
File.Data;                  // Property function.
File.GetUncompressedFile(); // Explicit function (alternative).

// To get the pure raw data which may be compressed under the hood use
File.RawData;
```

**Checking file properties**:
```public
// Use this even if your file is already decompressed.
File.FileSize;                  // Size of file (after decompression)
File.CompressedSize;            // Size of file before decompression (equal to FileSize if uncompressed)
File.CompressedFlag;            // Defines the compression method used on the current file.
```

**Setting File Properties**
```public
File.FileName = "Kondo";            // Renames the current file to "Kondo"
File.SetFileData(data, true);       // Replaces the data held internally by the File class
                                    // with new data, allowing you to change the underlying contents
                                    // without creating a new file.
                                    
                                    // This will automatically update the FileSize, CompressedSize
                                    // and CompressedFlag fields.
```
