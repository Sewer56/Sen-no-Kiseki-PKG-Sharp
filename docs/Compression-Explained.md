## Notes

**Last revised**: Oct 10 2018

**Author**: Sewer56

This guide is simplified and aims to explain the compression algorithm in very simple steps; such that it can be consumed by anyone - novice programmer, hobbyist, computer science student or professional.

It goes through the steps of decompression of a compressed PKG file; which complete with the decompressor source code `decompress.d` (written in the D language) should provide you with a full understanding of how to decompress a compressed file.

The structs used contain C# primitives; thus for those programmers using native languages should assume "byte" as "unsigned char".

## Some Notes about the Compression Algorithm

The compression is simple LZ77 based compression with Run Length Encoding (RLE); it's pretty much as simple as compression algorithms get with no bit packing involved and a clear block structure.

Although not benchmarked, simplicity of the format makes for some very; very fast decompression - it is very easy for it in fact to bottleneck a Solid State Drive if decompressing to disk.

## Basic Explanation

### Compressed File Structure
Each compressed file includes a 12 byte header; which includes an uncompressed file size, compressed file size (including the header) and a single byte (with padding) known as the "block key".

```csharp
struct FileHeader
{
    int uncompressedSize;   // Size of file after decompression
    int compressedSize;     // Length of compressed file (including 12 byte header)
    byte compressionKey;    // When this byte is seen; assume next compression block.
                            // Until compression block is seen; just copy raw bytes.
};
```

After the header is raw data for the length (in bytes) of `compressedSize - sizeof(FileHeader)` [this is compressedSize - 12]. This data is composed of simple bytes to be copied to another array (more on that later) and special instructions known as "compression blocks".

### The Decompressor: The Basics

The decompressor functions by working with two arrays; one array being the original array `source` containing the compressed file and the other array being created by the decompressor itself `destination`; equal to the decompressed size of the file `FileHeader.uncompressedSize`.

The raw data (after header) in `source` is read by the decompressor in sequential order from start to finish byte by byte and appended to `destination`; **unless the byte equals the "block key"** (`FileHeader.compressionKey`).

If the sequential order byte read encounters the "block key"; it assumes that the current byte is a start of a "compression block" - a special structure which instructs the decompressor that it should copy some bytes from elsewhere.

```csharp
// Not used in this template; for reference only.
struct CompressionBlock
{
   byte compressionKey;     // When this byte is seen; just copy raw bytes.
   byte offset;             // Where to copy from earlier in the file.
   byte length;             // Amount of bytes earlier in the file to copy from.
};
```

A "compression block" structure represents a very simple instruction for the decompressor; in informal terms it can be described as such:

"Hey; you see that last byte you wrote at the end of the `destination` array? Go back `offset` bytes in the array and add `length` bytes to the end of the array one by one."

The decompressor then resumes its normal operation; copying bytes from `source` to `destination` until the next "compression block".

*End of Array: Index of last added element in array + 1 (basically imagine the array as a vector/list and you are adding new elements).

### The Decompressor: A Special Case

Of course; if you only read the basic explanation above; you may have immediately noticed an inherent issue; `What if you want to add a byte equal to the compression key to the source?`

*The compression algorithm follows a `special rule` in order to prevent this clash.*

If a single byte equal to the compression key is to be added to the `source` and no decompression is meant to occur (handling of compression block) - the compression key is duplicated in the compressed file (*in other words the compression block offset is equal to compression key*).

This simple piece of semi-pseudocode explains the situation:
```csharp
// Get current byte.
byte currentByte = source[sourcePointer];

// Check if it's start of compression block.
if (currentByte == compressionKey)

    // Go to next byte and get offset.
    sourcePointer++;
    byte offset = source[sourcePointer];

    // If the 2nd byte matches unique compression key, just copy raw byte.
    // Otherwise perform regular decompression routine.
    if (offset == compressionBlockKey)
        AddCompressionKeyToSource();
    else 
        CopyWithLengthAndOffset(); 
```

As the introduction of this now prevents the offset from being equal to the compression key; a simple trick is used whereby if `offset >= compressionKey`; the offset is actually encoded as **1 greater** than the actual true offset.

Therefore the actual logic to get the offset and length in decompression would resemble this:

```csharp
CopyWithLengthAndOffset():
    // Go to next byte to get length and perform lz77 copy.
    sourcePointer++;
    byte length = source[sourcePointer];

    // This is a safeguard in the case that we want to have the offset equal the block key.
    // This effectively means that the max offset cannot be 255 (but 254).
    if (offset > compressionBlockKey)
        offset--;

    // Copies length bytes from destinationPointer - offset, length times to the end of destination.
    Copy(destination, destinationPointer, length, offset);
```

## Extra Details

### Sen no Kiseki: Additional Constraint

In Sen no Kiseki (Trails of Cold Steel) following additional constraint also applies:
- Length **CANNOT** exceed Offset.

This is because if that is true; bytes originally beyond the current source pointer (not set with correct value at the start of copying) will also be copied/appended to the end of the source array.

This is because under the hood, Sen no Kiseki makes use of more aggressive parallel code optimizations such as "Vectorization" to speed up decompression by allowing large size "vector" registers to be used for copying; in which case it would possible for them to contain invalid/unser bytes to copy if length > offset.

You may research the topics "Vectorization", "SIMD", "AVX" at your own disclosure.

### Compression: Selecting the Compression Key

Although this is a decompression guide; there was no other logical place to insert this bit of information.
For the current compression algorithm used; the compression key is simply selected by counting the occurrence of each byte in a file (occurences of 0-255) and the least used byte is selected.

This is done in order to reduce the frequency of having to insert two copies of the same byte to cancel a compression block and minimize final file size (see Special Case & Exception).
