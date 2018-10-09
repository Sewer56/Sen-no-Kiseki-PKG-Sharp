import std.stdio;
import std.file;
import std.datetime.stopwatch;
import decompress;
import compress;

void main()
{
    // Remember to change output type from DLL to EXE before testing.
    // Also remove the -shared additional commandline argument.
    decompressTest();
    compressTest();
    redecompressTest();

    readln();
}


void decompressTest()
{
	// Decompress
    string compressedName = "test.compressed";

    if (std.file.exists(compressedName))
    {
        byte[] compressed       = cast(byte[]) std.file.read(compressedName);

        byte[] decompressed;
        void decompbench() 
        { 
            decompressed = decompressWithHeader(compressed);
        }

        auto decompDurations = benchmark!(decompbench)(1);
        writeln("Decompress: " ~ decompDurations[0].toString());
        std.file.write("test.decompressed", decompressed);
    }
}

void compressTest()
{
    // Compress
    string decompressedName = "test.decompressed";

    if (std.file.exists(decompressedName))
    {
        byte[] decompressed     = cast(byte[]) std.file.read(decompressedName);

        byte[] compressed;
        void compbench() 
        { 
            auto returnValue = compressData(decompressed);
            compressed = (&returnValue[0])[0 .. returnValue.length].dup; 
        }

        auto decompDurations = benchmark!(compbench)(1);
        writeln("Compress: " ~ decompDurations[0].toString());
        std.file.write("test.recompressed", compressed);
    }
}

void redecompressTest()
{
    // Compress
    string recompressedName = "test.recompressed";

    if (std.file.exists(recompressedName))
    {
        byte[] recompressed     = cast(byte[]) std.file.read(recompressedName);

        byte[] decompressed;
        void redecompbench() 
        { 
            auto returnValue = decompressWithHeader(recompressed);
            decompressed = (&returnValue[0])[0 .. returnValue.length]; 
        }

        auto decompDurations = benchmark!(redecompbench)(1);
        writeln("Redecompress: " ~ decompDurations[0].toString());
        std.file.write("test.redecompressed", decompressed);
    }
}