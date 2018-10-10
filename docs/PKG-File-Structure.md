# Table of Contents
- [Package Files (PKG Layout)](#package-files-pkg-layout)
  - [Structures](#structures)
    - [Header Structure](#header-structure)
    - [File Entry Structure](#file-entry-structure)
  - [Structures -  Summary](#structures-summary)


## Package Files (PKG Layout)

A simple PKG/Package file consists of a simple `file header`; array of `file entries` and the raw data for the file entries themselves (compressed or uncompressed).

The PC game files that the following library has been made for are Little Endian.

### Structures

#### Header Structure

Always at the start of the file; size 0x8.

```csharp
struct FileHeader
{
    int unknownSignature;   // Some hash that is ignored by the game.
    int fileCount;          // Number of Files
};
```

#### File Entry Structure 

Following the header; this structure is repeated FileHeader.fileCount times.

```csharp
struct FileEntry 
{
    char fileName[64];      // Name of file (ANSI)
    int fileSize;           // Size of file before compression.
    int compressedSize;     // Size of file after compression.
							// This includes a 12 byte header if compressionType == 1.
							// Precisely this is a `fileLength` at the fileOffset.
							
    int fileOffset;         // Offset to file raw data (compressed or uncompressed).
                            // This offset is absolute; from start of file.
    
    int compressionType;    // 0 = Uncompressed; 
                            // 1 = Compressed (Type A)
                            // 2 = Compressed (Type B: Only seen in PS3 version)
};
```

### Structures -  Summary
All of the structures can be summarised as.

```csharp
FileHeader header;
FileEntry  entries[header.fileCount];
byte[] rawData;                     // Raw file data (compressed or uncompressed).
```

In addition; it should be mentioned that there is no padding for files; so the file offsets are not word size/4/8/16/32/<X> byte aligned.
