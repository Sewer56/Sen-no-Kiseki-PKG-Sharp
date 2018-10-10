module decompress;

/* 12 byte header inside the PKG prepended before the raw data. */
public struct FileHeader
{
    align (4):
        public int uncompressedSize;
        public int compressedSize;
        public ubyte compressionKey;
}

/**
    Decompresses a supplied array of Sen no Kiseki compression format 1
    compressed bytes prepended with a 12 byte header and returns a 
    decompressed copy of said bytes.

    12 Byte Header Description:
    uint uncompressedFileSize;
    uint compressedFileSize;
    uint compressionBlockKey;

    Params:
    source  =   An array of bytes read containing a compressed file.
    This SHOULD include the 12 byte header at the start of the
    file entries inside PKG files, then the raw compressed data after.
*/
public byte[] decompressWithHeader(byte[] source)
{
    FileHeader header           = *cast(FileHeader*)(&source[0]);
    byte[] rawCompressedData    = source[FileHeader.sizeof .. $];
    byte[] destination          = new byte[header.uncompressedSize];
    
    return decompressRaw(rawCompressedData, destination, header.compressionKey);
}

/**
    Decompresses a supplied array of Sen no Kiseki/PKG format 1
    compressed bytes and returns a decompressed copy of said bytes.

    Params:
    source      =   An array of bytes read containing a compressed file.
                    This SHOULD NOT include the 12 byte header at the start of the
                    file entries inside PKG files; only the raw compressed data after.
    destination =   The target array into which 

    compressionBlockKey    
                =   When this byte is seen; assume next compression block.
                    Until compression block is seen; just copy raw bytes.
*/
public byte[] decompressRaw(byte[] source, byte[] destination, ubyte compressionBlockKey)
{
    /** This is the pointer inside the source array where we will either receive an opcode or copy raw byte from. */
    int sourcePointer = 0;

    /** This is the pointer inside the destination array the next byte(s) will be written to. */
    int destinationPointer = 0;

    // Decompress
    for (; sourcePointer < source.length; sourcePointer++)
    {
        // Get current byte.
        ubyte currentByte = source[sourcePointer];

        // Check if it's start of compression block.
        if (currentByte == compressionBlockKey)
        {
            // Go to next byte and get offset.
            sourcePointer++;
            ubyte offset = source[sourcePointer];

            // If the 2nd byte matches unique compression key, just copy raw byte.
            // The compressor supports placing the block key twice in succession in order to not
            // perform a copy.

            if (offset == compressionBlockKey)
            {
                // Direct Byte Copy
                destination[destinationPointer] = currentByte;
                destinationPointer++;
            }
            else 
            {
                // Go to next byte to get length and perform lz77 copy.
                sourcePointer++;
                ubyte length = source[sourcePointer];

                // This is a safeguard in the case that we want to have the offset equal the block key.
                // This effectively means that the offset cannot be 255 (max 254).
                if (offset > compressionBlockKey)
                    offset--;

                lz77Copy(destination, destinationPointer, length, offset);
            }
        }
        else 
        {
            // Direct Byte Copy
            destination[destinationPointer] = currentByte;
            destinationPointer++;
        }

    }

    return destination;
}

/**
    Copies bytes from the source array to the destination array with the specified length
    and offset. The final byte index of the destination is used for declaring the position from which
    the look behind operation is performed.
*/
public void lz77Copy(byte[] destination, ref int destinationPointer, int length, int offset)
{
	// Contains the pointer to the destination array from which to perform the look back for bytes to copy from.
	int copyStartPosition = destinationPointer - offset; // offset is positive

    // Vector optimizations; this compresison format does not support overlaps.
    destination[destinationPointer .. destinationPointer + length] = destination[copyStartPosition .. copyStartPosition + length];   

    destinationPointer += length;
}
