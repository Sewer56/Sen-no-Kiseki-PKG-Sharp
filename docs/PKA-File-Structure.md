# Table of Contents
- [Package Archive Files (PKA Layout)](#package-archive-files-pka-layout)
  - [Structures](#structures)
    - [Header Structure](#header-structure)
    - [File Entry Structure](#file-entry-structure)
    - [Compression Entry Structure](#compression-entry-structure)

## Package Archive Files (PKA Layout)

The PKA/Package archive file consists of a `signature` a repeating array of `package headers` along with its corresponding `file entries`; an array of `compression entries` and the raw data for the file entries themselves (compressed or uncompressed).

The PC game files that the following records document are Little Endian.

### Structures

#### Header Structure

Always at the start of the file; size 0x8.

```csharp
struct PKAHeader
{
    int signature;           // Value that is verified for CSIII v1.05 PC it is 0x7ff7cf0d
    int entryCount;          // Number of Entries
};
```

#### File Entry Structure 

Following the Package Archive header; this structure is followed by a File Entry Structure. This Package Header with File Entries structure is repeated PKAHeader.entryCount times.

#Package Header

```csharp
struct PkgHeader
{
    char fileName[32];      // Name of package (ANSI)
    int fileCount;          // Number of files
};
```

#### File Entry Structure 

Following the Package header; this structure is repeated PkgHeader.fileCount times.

```csharp
struct FileEntry 
{
    char fileName[64];       // Name of file (ANSI)
    char fileSHA256Hash[32]; // SHA256 Hash of file
};
```

Following the final Package Header along with its  File Entry Structure are the Compression Entry Structures

#### Compression Entry Structure 
```csharp
struct CompressionEntry 
{
    char fileSHA256Hash[32]; // SHA256 Hash of file this is the same as in the File Entry
    ulong fileOffset;        // Offset to file raw data (compressed or uncompressed).
                             // This offset is absolute; from start of package archive.
    uint compressedSize;     // Size of file after compression.
    uint fileSize;           // Size of file before compression.
    int compressionType;     // 0 = Uncompressed; 
                             // 1 = Compressed (Type A)
                             // 2 = Compressed (Type B: Only seen in PS3 version)
                             // 4 = Compressed with LZ4
};
```

Compression Data then follows until the end of the archive file.
