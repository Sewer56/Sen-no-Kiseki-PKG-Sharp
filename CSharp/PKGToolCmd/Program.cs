using System;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PKGLib;

namespace PKGToolCmd
{
    class Program
    {
        /* Flags */
        private static bool _multiThreaded   = true;
        private static bool _noCompress      = false;
        private static bool _batchExtract    = false;
        private static bool _batchBuild   = false;

        private static ParallelOptions _parallelOptions = new ParallelOptions();

        /* Multithreading lock */
        private static readonly object LockObject = new object();

        /* Entry Point */
        private static void Main(string[] args)
        {
            // Set arguments.
            if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
                ShowHelp();

            // Set arguments.
            for (int x = 0; x < args.Length; x++)
            {
                if (args[x] == "--nocompress")      _noCompress      = true;
                if (args[x] == "--singlethread")    _multiThreaded   = false;
                if (args[x] == "--batchextract")    _batchExtract    = true;
                if (args[x] == "--batchbuild")      _batchBuild      = true;
            }

            // To be used if multithreaded.
            // Value obtained with a 4790k hyperthreaded processor and a regular SATA III SSD with trial and error.
            // Basically we want to use enough threads to the point that we can still perform operations
            // while the IO is bottlenecking.
            _parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * 3;

            // Compress/Extract PKG
            for (int x = 0; x < args.Length; x++)
            {
                // File Extract Operation
                if (File.Exists(args[x]))
                    Extract(args[x], _multiThreaded);

                // Directory Operations
                if (Directory.Exists(args[x]))
                {
                    if (_batchBuild)
                        RunWithGcLowLatency(() => BatchCompress(args[x]));
                    else if (_batchExtract)
                        RunWithGcLowLatency(() => BatchExtract(args[x]));
                    else
                        Build(args[x], _multiThreaded);
                }
            }
        }

        /// <summary>
        /// Runs a function with low Garbage Collector latency.
        /// Garbage collection will only run when under heavy memory constraints.
        /// </summary>
        /// <param name="action">The function to execute.</param>
        private static void RunWithGcLowLatency(Action action)
        {
            GCLatencyMode oldMode = GCSettings.LatencyMode;

            // Make sure we can always go to the catch block, 
            // so we can set the latency mode back to `oldMode`
            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                #if DEBUG
                Console.WriteLine("Starting NoGC region.");
                #endif
                StartNoGCRegion(true);

                #if DEBUG
                Console.WriteLine("Performing Action.");
                #endif

                // Generation 2 garbage collection is now
                // deferred, until the allocated limit is broken.
                action();


                #if DEBUG
                Console.WriteLine("Ending NoGC region.");
                #endif

                if (GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
                    GC.EndNoGCRegion();
            }
            finally
            {
                // Collect garbage and set the latency mode back
                GC.Collect();
                GCSettings.LatencyMode = oldMode;
            }
        }

        /// <summary>
        /// Attempts to start a no garbage collection region.
        /// </summary>
        private static void StartNoGCRegion(bool disallowBlockingCollection)
        {
            bool result = false;

            try { result = GC.TryStartNoGCRegion(2000000000, disallowBlockingCollection); }       // 2000 MB 
            catch { }

            if (result == false)
            {
                try   { result = GC.TryStartNoGCRegion(240000000, disallowBlockingCollection); }       // 240 MB 
                catch { }
            }

            if (result == false)
            {
                try   { result = GC.TryStartNoGCRegion(100000000, disallowBlockingCollection); }   // 100 MB 
                catch { }
            }

            if (result == false)
            {
                try   { result = GC.TryStartNoGCRegion(16000000, disallowBlockingCollection); }    // 16 MB 
                catch { }
            }
        }

        /// <summary>
        /// Extracts each PKG file within a specified directory.
        /// </summary>
        private static void BatchExtract(string directoryPath)
        {
            string fullDirectoryPath    = Path.GetFullPath(directoryPath);
            string[] files              = Directory.GetFiles(fullDirectoryPath);

            if (_multiThreaded)
            {
                // Multithreaded extraction of singlethreaded extract operations.
                Parallel.ForEach(files, _parallelOptions, file =>
                {
                    Extract(file, false);
                });
            }
            else
            {
                // Singlethreaded extraction of singlethreaded extract operations.
                for (int x = 0; x < files.Length; x++)
                {
                    Extract(files[x], false);
                }
            }
        }

        /// <summary>
        /// Builds a PKG file for each directory within a passed in directory.
        /// </summary>
        private static void BatchCompress(string directoryPath)
        {
            string fullDirectoryPath    = Path.GetFullPath(directoryPath);
            string[] directories        = Directory.GetDirectories(fullDirectoryPath);

            // Sorting by file size is an optimization that ensures that at the end of compression a thread
            // will not be left with a large file
            directories                 = directories.OrderByDescending(GetDirectorySize).ToArray();

            if (_multiThreaded)
            {
                // Multithreaded building of singlethreaded package builds.
                Parallel.ForEach(directories, _parallelOptions, directory =>
                {
                    Build(directory, false);
                });
            }
            else
            {
                // Singlethreaded building of singlethreaded package builds.
                foreach (var directory in directories)
                    Build(directory, false);
            }
        }

        /// <summary>
        /// Creates a subdirectory and extracts all of the files within a specified PKG archive.
        /// </summary>
        /// <param name="filePath">Path to a PKG file.</param>
        /// <param name="multiThreaded">Set to true to extract each file in a multithreaded fashion.</param>
        private static void Extract(string filePath, bool multiThreaded)
        {
            // Get path to file and create new directory.
            string fullFilePath     = Path.GetFullPath(filePath);
            string directoryName    = $"{Path.GetDirectoryName(fullFilePath)}\\{Path.GetFileNameWithoutExtension(fullFilePath)}";
            Directory.CreateDirectory(directoryName);

            // Extract file to directory.
            Archive archive = new Archive(fullFilePath);

            // Populate Archive
            if (multiThreaded)
            {
                Parallel.ForEach(archive.Files, _parallelOptions, file =>
                {
                    #if DEBUG
                    Console.WriteLine($"Extracting: {directoryName}\\{file.FileName}");
                    #endif

                    File.WriteAllBytes($"{directoryName}\\{file.FileName}", file.Data);
                });
            }
            else
            {
                foreach (var file in archive.Files)
                {
                    #if DEBUG
                    Console.WriteLine($"Extracting: {directoryName}\\{file.FileName}");
                    #endif
                    File.WriteAllBytes($"{directoryName}\\{file.FileName}", file.GetUncompressedFile());
                }
                
            }
        }

        /// <summary>
        /// Creates a PKG from a directory and places it in a subdirectory of the directory.
        /// </summary>
        /// <param name="directoryPath">Path to a directory to create a PKG archive from.</param>
        /// <param name="multiThreaded">Set to true to add each file in a multithreaded fashion.</param>
        private static void Build(string directoryPath, bool multiThreaded)
        {
            // Gets full path and all files in directory.
            string fullDirectoryPath    = Path.GetFullPath(directoryPath);
            string[] filesInDirectory   = Directory.GetFiles(fullDirectoryPath);

            // Inherit file name from directory name and set file path.
            string fileName             = Path.GetFileNameWithoutExtension(fullDirectoryPath) + ".pkg";
            string newDirectoryPath     = Path.GetDirectoryName(fullDirectoryPath); // Directory folder is contained within.
            string savePath             = $"{newDirectoryPath}\\{fileName}";

            Archive archive             = new Archive();

            // Populate Archive
            if (multiThreaded)
            {
                Parallel.ForEach(filesInDirectory, _parallelOptions, filePath =>
                {
                    #if DEBUG
                    Console.WriteLine($"Creating File: {filePath}");
                    #endif
                    var pkgFile = new PKGLib.Definitions.Managed.File(filePath, !_noCompress);

                    lock (LockObject)
                    { archive.Files.Add(pkgFile); }
                });
            }
            else
            {
                foreach (string filePath in filesInDirectory)
                {
                    #if DEBUG
                    Console.WriteLine($"Creating File: {filePath}");
                    #endif
                    archive.Files.Add(new PKGLib.Definitions.Managed.File(filePath, !_noCompress));
                }
                
            }

            archive.Save(savePath);
        }

        /// <summary>
        /// Returns the individual size of all files contained inside a directory.
        /// </summary>
        /// <param name="directory">The directory to get file size from.</param>
        /// <returns></returns>
        static long GetDirectorySize(string directory)
        {
            // Get array of all file names.
            string[] allFiles = Directory.GetFiles(directory, "*.*");

            // Calculate total bytes of all files.
            long totalBytes = 0;
            foreach (string name in allFiles)
            {
                FileInfo info = new FileInfo(name);
                totalBytes += info.Length;
            }

            return totalBytes;
        }

        /// <summary>
        /// Prints the help notes to the console.
        /// </summary>
        static void ShowHelp()
        {
            #if DEBUG
            Console.WriteLine("[DEBUG BUILD]");
            #endif
            Console.WriteLine("=> Sen no Kiseki PKG Archive Utility by Sewer56");
            Console.WriteLine("=> Usage:");
            Console.WriteLine("Extract:   PKGToolCmd.exe <file>");
            Console.WriteLine("Extract (Batch): PKGToolCmd.exe <directory containing PKGs> --batchextract");
            Console.WriteLine("Build PKG: PKGToolCmd.exe <directory>");
            Console.WriteLine("Build PKG (Batch): PKGToolCmd.exe <directory containing directories> --batchcompress");
            Console.WriteLine("=> Flags:");
            Console.WriteLine("--singlethread   : Singlethreaded building of PKG files.");
            Console.WriteLine("--nocompress     : Disables compression for the building of PKG archive files.");
            Console.WriteLine("--batchbuild     : Builds a PKG file for each directory within a passed in directory.");
            Console.WriteLine("--batchextract   : For each PKG file in directory; extract the PKG file.");
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}
