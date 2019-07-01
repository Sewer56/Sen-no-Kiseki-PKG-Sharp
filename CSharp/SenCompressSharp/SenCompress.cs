using System;
using System.Runtime.InteropServices;

namespace SenCompressSharp
{
    /// <summary>
    /// Provides a means by which the underlying D language library's functions may be accessed.
    /// </summary>
    public static unsafe class SenCompress
    {
        /* Function redirects to X64/X86 DLL */
        private static Action<byte[], int, int, CopyArrayFunction>    _compressFunction;
        private static Action<byte[], int, byte[]>                    _decompressFunction;
        private static Action<IntPtr, int, byte[]>                    _decompressFunctionAlt;

        /* 12 Byte Header definition for a compressed file. */
        struct FileHeader
        {
            public int  UncompressedSize;
            public int  CompressedSize;
            public byte CompressionKey;
        }
        
        /* Defines a C# function which fills a managed array using a pointer and length. */
        /// 
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CopyArrayFunction(IntPtr dataPtr, int length);

        // Static initializer that sets up redirects to X86/X64 functions.
        static SenCompress()
        {
            if (IntPtr.Size == 4)
            {
                _compressFunction   = SenCompress32.Compress;
                _decompressFunction = SenCompress32.Decompress;
                _decompressFunctionAlt = SenCompress32.Decompress;

            }
            else if (IntPtr.Size == 8)
            {
                _compressFunction   = SenCompress64.Compress;
                _decompressFunction = SenCompress64.Decompress;
                _decompressFunctionAlt = SenCompress64.Decompress;
            }
            else
            {
                throw new NotSupportedException("SenCompress compression library is only compiled for X86 and X86_64 architectures.");
            }
        }

        /// <summary>
        /// Compresses a supplied byte array.
        /// Returns the compressed version of the byte array.
        /// </summary>
        /// <param name="data">The byte array containing the file or data to compress.</param>
        /// <param name="searchBufferSize">(Default = 254)
        /// A value preferably between 0 and 254 that declares how many bytes
        /// the compressor visit before any specific byte to search for matching patterns.
        /// Increasing this value compresses the data to smaller filesizes at the expense of compression time.
        /// Changing this value has no noticeable effect on decompression time.</param>
        /// <returns></returns>
        public static byte[] Compress(byte[] data, int searchBufferSize = 254)
        {
            byte[] resultantBytes = new byte[0];
            void AllocateArrayImpl(IntPtr dataPtr, int length)
            {
                resultantBytes = new byte[length];
                Marshal.Copy(dataPtr, resultantBytes, 0, length);
            }

            _compressFunction(data, data.Length, searchBufferSize, AllocateArrayImpl);
             return resultantBytes;
        }

        /// <summary>
        /// Decompresses a supplied array of PKG Type 1 (RLE) compressed file bytes
        /// (including 12 byte header) and returns a decompressed copy.
        /// </summary>
        /// <param name="data">The individual RLE compressed data with header to decompress.</param>
        /// <returns></returns>
        public static unsafe byte[] Decompress(byte[] data)
        {
            fixed (byte* dataPtr = data)
            {
                FileHeader header   = *(FileHeader*)dataPtr;
                byte[] target       = new byte[header.UncompressedSize];
                _decompressFunction(data, data.Length, target);
                return target;
            }
        }

        /// <summary>
        /// Decompresses a supplied array of PKG Type 1 (RLE) compressed file bytes
        /// (including 12 byte header) and returns a decompressed copy.
        /// </summary>
        /// <param name="dataPtr">The pointer to individual RLE compressed data with header.</param>
        /// <param name="length">The length of memory referenced by the pointer.</param>
        /// <returns></returns>
        public static unsafe byte[] Decompress(byte* dataPtr, int length)
        {
            FileHeader header = *(FileHeader*)dataPtr;
            byte[] target = new byte[header.UncompressedSize];
            _decompressFunctionAlt((IntPtr)dataPtr, length, target);
            return target;
        }
    }
}
