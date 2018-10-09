using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace SenCompressSharp
{
    public static unsafe class SenCompress64
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport("SenCompress64.dll",
            EntryPoint = "externCompress",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern void Compress(byte[] data, int length, int searchBufferSize, SenCompress.CopyArrayFunction allocateArrayFunction);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("SenCompress64.dll",
            EntryPoint = "externDecompress",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern void Decompress(byte[] data, int length, byte[] destination);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("SenCompress64.dll",
            EntryPoint = "externDecompress",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        public static extern void Decompress(IntPtr data, int length, byte[] destination);
    }
}
