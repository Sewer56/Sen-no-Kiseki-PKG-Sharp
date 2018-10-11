module exports;

import core.memory;
import core.stdc.stdlib;
import core.stdc.string;

import compress;
import decompress;


// Callback to calling code (in CDECL convention) that allocates and copies to their own array from 
// a byte* and length combo.
extern(C) alias CopyMemoryFunctionDefinition = void function(byte*, int);  // D code

/**
	Compresses a supplied byte array.
	Returns the compressed version of the byte array including the 12 byte header of a PKG file.
	After you are done with using the byte array returned, you should call the exported function
	clearFiles() to dispose of the leftover memory returned to you.

	Params:
	data =					Pointer to the start of the byte array of the file to decompress.
	length =				The length of the byte array.

	searchBufferSize =		(Default: 254)

	A value preferably between 0x0 and 254 that declares how many bytes
	the compressor visit before any specific byte to search for matching patterns.
	Increasing this value compresses the data to smaller filesizes at the expense of compression time.
	Changing this value has no noticeable effect on decompression time.

    allocateMemoryPtr =     A pointer to a CDECL function that copies a region of memory back to its own
                            buffer given a pointer and length.

*/
export extern(C) void externCompress(byte* data, int length, int searchBufferSize, CopyMemoryFunctionDefinition copyMemoryPtr)
{
	byte[] passedData = data[0 .. length];	
	auto compressedData = compressData(passedData, searchBufferSize);

    copyMemoryPtr(&compressedData[0], cast(int)compressedData.length);
}

/**
	Decompresses a supplied byte array by filling a supplied target array where the data should
    be decompressed.

	Params:
	source =				The byte array containing the file or data to decompress (including 12 byte header).
	length =				The length of the byte array.
    target =                The target array to fill.
*/
export extern(C) void externDecompress(byte* data, int length, byte* target)
{
    FileHeader header = *cast(FileHeader*)(data);

	byte[] passedData = data[FileHeader.sizeof .. length];
    byte[] targetData = target[0 .. header.uncompressedSize];

	decompressRaw(passedData, targetData, header.compressionKey);
}