# About This Repository
This repository consists of the following components: 

- **PKGLib:** A C# library for handling PKG files .
- **dlang-sencompress:** A high performance library for Sen no Kiseki Compression/Decompression.
- **SenCompressSharp:** A C# wrapper for the dlang-sencompress library.
- **PKGToolCmd**: A simple C# commandline utility for extracting and packing of Sen no Kiseki PKG files.

![Exhibit A](https://i.imgur.com/2xd34rH.png)

The compression library component is implemented using the [D Programming Language](https://dlang.org/) and is based off of my own PKG Compressor [dlang-prs](https://github.com/sewer56lol/dlang-prs).

The _PKGLib_ and _SenCompress_ libraries are written in _.NET Standard_ thus can be used with any .NET implementation such as Framework, Core, Xamarin or Mono.

## Documentation

- [PKG File Structure](https://github.com/sewer56lol/Sen-no-Kiseki-PKG-Sharp/blob/master/docs/PKG-File-Structure.md)
- [Compression Explained](https://github.com/sewer56lol/Sen-no-Kiseki-PKG-Sharp/blob/master/docs/Compression-Explained.md)
- [Using the Libraries](https://github.com/sewer56lol/Sen-no-Kiseki-PKG-Sharp/blob/master/docs/Using-The-Libraries.md)

## Manually compiling this repository.

In order to build this project; make sure that you have [Visual D](https://github.com/dlang/visuald) installed as well as the multilib version of [LDC](https://github.com/ldc-developers/ldc/releases).

Following; ensure that LDC is correctly configured inside Visual Studio ![Image](https://i.imgur.com/Fwjc67d.png).

To compile the C# components; you will require the .NET Core SDK; the version used in this repository at the time of writing is .NET Core 2.1. You can either install it manually or inside the Visual Studio installer.

Once you are done; simply open `dlang-sencompress.sln` inside the `D` folder.

Note: You may need to adjust the output path in your project properties!

## Adding the libraries to your own project.

Do note that in order to use the actual libraries in your own projects you do not actually need the D compiler. 
Individual precompiled packages are available on NuGet; you will only need the compiler should you choose to contribute to the repository.

For more details see "Using the Libraries".
