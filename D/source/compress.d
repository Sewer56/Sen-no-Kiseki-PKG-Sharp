module compress;

import std.math; 
import std.typecons;
import std.container.array;
import decompress;

/* Maximum length the compression block is allowed to match. */
private const int maxLength = 254;

/** 
	Defines a quick structure in the form of a tuple which
	declares the properties common to LZ77 compression algorithms.
*/
alias LZ77Properties = Tuple!(int, "offset", int, "length");

/**
	Compresses a supplied byte array with PKG Compression type 1.
	Returns the compressed version of the byte array.

	Params:
		source =				The byte array containing the file or data to compress.

		searchBufferSize =		(Default: 254)
								
                                The allowed range of values range from 0-254 and decide the maximum size of the search
                                buffer for copying data from.

                                Large values compress the data to smaller filesizes at the expense of compression time.
								Changing this value has no noticeable effect on decompression time.
*/
public Array!byte compressData(byte[] source, int searchBufferSize = 254)
{
    // Assume our compressed file will be at least of equivalent length.
	auto destination = Array!byte();
	destination.reserve( cast(int)(source.length) );

    // Set variables.
    int      sourcePointer      = 0;
    ubyte    compressionByte    = GetLeastCommonByte(source);

    // Reserve space for header.
    destination.insert(new byte[FileHeader.sizeof]);

    // Begin compression.
    while (sourcePointer < source.length)
    {
        // Find the longest match of repeating bytes.
		LZ77Properties lz77Match = lz77GetLongestMatchNoOverlap(source, sourcePointer, searchBufferSize, maxLength);

		// Pack into the archive as direct byte if the amount of bytes to copy is
        // within the size of the block.
		if (lz77Match.length < 4)
		{
            // Copy direct byte.
            ubyte nextByte = source[sourcePointer];  // Get remote byte and store locally.
            destination.insert(nextByte);
            
            // If the direct byte is equal to the compression key; copy another byte.
            // Insert same byte as offset.
            if (nextByte == compressionByte)
                destination.insert(nextByte);

            sourcePointer += 1;
		}
		else
		{
			// Write a new compression block.
            destination.insert(compressionByte);
            
            // Adjust offset if necessary.
            // This is a safeguard in the case that we want to have the offset equal the block key.
            // This effectively means that the offset cannot be 255 (max 254).
            if (lz77Match.offset >= compressionByte)
                lz77Match.offset += 1;

            // Append offset and length.
            destination.insert(cast(ubyte)lz77Match.offset);
            destination.insert(cast(ubyte)lz77Match.length);

            // Increment our pointer to new location to source from after the copy.
            sourcePointer += lz77Match.length;
		}
    }

    // Generate header.
    FileHeader header;
    header.uncompressedSize = cast(int) source.length;
    header.compressedSize   = cast(int) destination.length;
    header.compressionKey   = compressionByte;
    
    // Write to first 12 bytes.
    byte[] headerSlice      = (cast(byte*) &header)[0 .. FileHeader.sizeof];
    byte[] destinationSlice = (cast(byte*) &(destination[0]))[0 .. FileHeader.sizeof];

    destinationSlice[]      = headerSlice[];

    // Return back
	return destination; 
}

/**
	Digs through the search buffer and finds the longest match
	of repeating bytes which match the bytes at the current pointer
	onward.

	Params:
		source = Defines the array in which we will be looking for matches.
		pointer = Specifies the current offset from the start of the file used for matching symbols from.
		searchBufferSize = The amount of bytes to search backwards in order to find the matching pattern.
		maxLength = The maximum number of bytes to match in a found pattern searching backwards. This number is inclusive, i.e. includes the passed value.
	
*/
public LZ77Properties lz77GetLongestMatchNoOverlap(byte[] source, int pointer, int searchBufferSize, int maxLength)
{
	/*	The source bytes are a reference in order to prevent copying. 
		The other parameters are value type in order to take advantage of locality of reference.
	*/
	
	/** Stores the details of the best found LZ77 match up till a point. */
	LZ77Properties bestLZ77Match    = LZ77Properties(0,0);

	/** ---------------------------
		Properties of the for loop.
		---------------------------

		Simplifies the loop itself and prevents unnecessary 
		calculations on every step of the loop.
	*/  

	/** The pointer inside the search buffer at which to start searching repeating bytes for. */
	int currentPointer = pointer - 1;

	/** The length of the current match of symbols. */
	int currentLength = 0;

	/** Set the minimum position the pointer can access. */
	int minimumPointerPosition = pointer - searchBufferSize;
	if (minimumPointerPosition < 0)
		minimumPointerPosition = 0;

	/** Iterate over each individual byte backwards to find the longest match. */
	for (; currentPointer >= minimumPointerPosition; currentPointer--)
	{
		if (source[currentPointer] == source[pointer])
		{
			// We've matched a symbol.
			currentLength = 1;
			
			/* Check for matches. */
			while ((source[currentPointer + currentLength] == source[pointer + currentLength]) && 
                   (pointer + currentLength < source.length) && 
                    currentPointer + currentLength < pointer)
			{
				currentLength++;
			}

			/* 
				Set the best match if acquired.
			*/
			if (currentLength > bestLZ77Match.length)
			{
				bestLZ77Match.length = currentLength;
				bestLZ77Match.offset = pointer - currentPointer;
			}
		}
	}

	return bestLZ77Match;
}

/**
    Returns the individual byte which has the least frequency within the 
    byte array.

    This generates/forms our byte we will use as a key.
*/
public ubyte GetLeastCommonByte(byte[] source)
{
    // Initialize occurence count of each byte.
    uint[256] byteOccurences;

    // Get occurences of each byte.
    for (int x = 0; x < source.length; x++)
    {
        ubyte currentByte = source[x];
        byteOccurences[currentByte] += 1;
    }

    // Get lowest occurence of each byte.
    ubyte indexWithLeast  = 0;
    uint lowestCount      = uint.max;

    for (int x = 0; x < byteOccurences.length; x++)
    {
        if (byteOccurences[x] < lowestCount)
        {
            indexWithLeast  = cast(ubyte)x;
            lowestCount     = byteOccurences[x];
        }    
    }
    
    return indexWithLeast;
}