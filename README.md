# About This Repository
This repository consists of the following components: 

- **PKGLib:** A C# library for handling PKG files .
- **dlang-sencompress:** A high performance library for Sen no Kiseki Compression/Decompression.
- **SenCompressSharp:** A C# wrapper for the dlang-sencompress library.
- **PKGToolCmd**: A simple C# commandline utility for extracting and packing of Sen no Kiseki PKG files.

![Exhibit A](https://i.imgur.com/2xd34rH.png)

The compression library component is implemented using the [D Programming Language](https://dlang.org/) and is based off of my own PKG Compressor [dlang-prs](https://github.com/sewer56lol/dlang-prs).

The _PKGLib_ and _SenCompress_ libraries are written in _.NET Standard_ thus can be used with any .NET implementation such as Framework, Core, Xamarin or Mono.

## A Short Introduction

The following is a short summary of the PKG archive format layout; feel free to use this both for educational reasons and as a reference for wishing to implement their own library or parser.

Both the compression format and PKG structure are very simple; up to the point I'd personally consider them a great resources for anyone willing to start reverse engineering file formats.

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

## About the Compression Algorithm

// Coming later today.

## Compiling

In order to build this project; make sure that you have [Visual D](https://github.com/dlang/visuald) installed as well as the multilib version of [LDC](https://github.com/ldc-developers/ldc/releases).

Following; ensure that LDC is correctly configured inside Visual Studio ![Image](https://i.imgur.com/Fwjc67d.png).

To compile the C# components; you will require the .NET Core SDK; the version used in this repository at the time of writing is .NET Core 2.1. You can either install it manually or inside the Visual Studio installer.
