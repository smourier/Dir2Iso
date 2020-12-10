using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using IMAPI2FS;

namespace Dir2Iso
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Dir2Iso - Copyright (C) 2019-" + DateTime.Now.Year + " Simon Mourier. All rights reserved.");
            Console.WriteLine();
            if (CommandLine.HelpRequested || args.Length < 2)
            {
                Help();
                return;
            }

            var inputDirectoryPath = CommandLine.GetArgument<string>(0);
            var outputFilePath = CommandLine.GetArgument<string>(1);
            if (inputDirectoryPath == null || outputFilePath == null)
            {
                Help();
                return;
            }

            var format = FsiFileSystems.FsiFileSystemUDF;
            var formatString = CommandLine.GetNullifiedArgument("format");
            if (formatString != null)
            {
                format = Conversions.ChangeType("FsiFileSystem" + formatString, FsiFileSystems.FsiFileSystemNone);
                if (format != FsiFileSystems.FsiFileSystemISO9660 &&
                    format != FsiFileSystems.FsiFileSystemJoliet &&
                    format != FsiFileSystems.FsiFileSystemUDF)
                {
                    Help();
                    return;
                }
            }

            var image = new MsftFileSystemImage();
            image.ChooseImageDefaultsForMediaType(IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DISK);
            image.FileSystemsToCreate = format;
            image.Root.AddTree(inputDirectoryPath, false);

            var result = image.CreateResultImage();

            const int STATFLAG_NONAME = 0x1;
            result.ImageStream.Stat(out var stat, STATFLAG_NONAME);

            var exePath = Process.GetCurrentProcess().MainModule.FileName;
            var filePath = Path.Combine(Path.GetDirectoryName(exePath), outputFilePath);

            const int STGM_READWRITE = 0x2;
            const int STGM_CREATE = 0x1000;
            var hr = SHCreateStreamOnFile(filePath, STGM_READWRITE | STGM_CREATE, out var stream);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            result.ImageStream.RemoteCopyTo(stream, stat.cbSize, out var read, out var written);
            Console.WriteLine("Finished. Read: " + read.QuadPart + " bytes. Written: " + written.QuadPart + " bytes.");
        }

        [DllImport("shlwapi", CharSet = CharSet.Unicode)]
        private static extern int SHCreateStreamOnFile(string pszFile, int grfMode, out FsiStream stream);

        static void Help()
        {
            Console.WriteLine(Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant() + " <input directory path> <output .iso file path> [...options...]");
            Console.WriteLine();
            Console.WriteLine("Description:");
            Console.WriteLine("    This tool is used to create a .ISO file from a directory path.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("    /format:<format>       Defines the ISO format. It can be UDF, Joliet or Iso9660. Default value is UDF.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine();
            Console.WriteLine("    " + Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant() + " c:\\mypath\\myfiles myfiles.iso /format:Iso9660");
            Console.WriteLine();
            Console.WriteLine("    Creates a myfiles.iso ISO file with the Iso9660 format");
            Console.WriteLine();
        }
    }
}
