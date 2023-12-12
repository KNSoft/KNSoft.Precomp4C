using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Collections.Generic;
using Microsoft.Build.Framework;

using KNSoft.C4Lib.PEImage;

namespace KNSoft.Precomp4C;

public class LibExtractTask : Precomp4CTask
{
    [Required]
    public required String[] SearchPaths { get; set; }

    [Required]
    public required String OutputDirectory { get; set; }

    [Required]
    public required String Platform { get; set; }

    [Output]
    public required String[] ExtractedFiles { get; set; }

    public override Boolean Execute()
    {
        try
        {
            XmlDocument doc = new();
            doc.Load(Source);

            if (doc.DocumentElement == null)
            {
                throw new InvalidDataException("Invalid XML file: " + Source);
            }

            if (doc.DocumentElement.Name != "LibExtract")
            {
                throw new InvalidDataException("Invalid root element: " + doc.DocumentElement.Name);
            }

            DirectoryInfo OutputDirectoryInfo = Directory.CreateDirectory(OutputDirectory);
            List<String> ExtractedObjectFiles = [];

            foreach (XmlElement Lib in doc.DocumentElement.GetElementsByTagName("Lib").OfType<XmlElement>())
            {
                XmlAttributeCollection LibAttr = Lib.Attributes;
                String LibName = LibAttr["Name"]?.Value ?? throw new ArgumentException("Lib 'Name' unspecified in: " + Lib.OuterXml);

                String? Archs = LibAttr["Arch"]?.Value;
                if (Archs != null)
                {
                    IMAGE_FILE_MACHINE Machine = FileHeader.GetMachineType(Platform);
                    if (!Array.Exists(Archs.Split(' '), x => FileHeader.GetMachineType(x) == Machine))
                    {
                        continue;
                    }
                }

                FileInfo[] Libs = [];
                foreach (String SearchPath in SearchPaths)
                {
                    DirectoryInfo SearchDirectory = new DirectoryInfo(SearchPath);
                    FileInfo[] Files = SearchDirectory.GetFiles(LibName);
                    Libs = [.. Libs, .. Files];
                }
                if (Libs.Length == 0)
                {
                    throw new FileNotFoundException("Library " + LibName + " not found in specified directories");
                } else if (Libs.Length > 1)
                {
                    throw new FileNotFoundException("Multiple " + LibName + " found in specified directories");
                }

                Byte[] LibData = File.ReadAllBytes(Libs[0].FullName);
                ArchiveFile Ar = new(LibData);
                DirectoryInfo OutputLibDirectoryInfo = Directory.CreateDirectory(OutputDirectory + Path.DirectorySeparatorChar + Libs[0].Name);
                List<ArchiveFile.Import> ExtractImports = [];
                foreach (XmlElement ObjectFile in Lib.GetElementsByTagName("Object").OfType<XmlElement>())
                {
                    XmlAttributeCollection ObjectAttr = ObjectFile.Attributes;
                    String ObjectName = ObjectAttr["Name"]?.Value ?? throw new ArgumentException("Object 'Name' unspecified in: " + ObjectFile.OuterXml);
                    Predicate<ArchiveFile.Import>[] Match =
                    [
                        x => Path.GetFileName(x.Name) == ObjectName,
                        x => x.SymbolNames.Length > 0 && x.Offset > 0 && x.Data.Length > 0,
                        x => x.Name == ObjectName
                    ];
                    ExtractImports.Add(FilterImports(Ar.Imports, Match, 0, "No matched object name \"" + ObjectName + "\" found in library " + Libs[0].FullName));
                }

                foreach (ArchiveFile.Import ExtractImport in ExtractImports)
                {
                    String OutputObjectFilePath = OutputLibDirectoryInfo.FullName +
                                                  Path.DirectorySeparatorChar +
                                                  Path.GetFileName(ExtractImport.Name);
                    File.WriteAllBytes(OutputObjectFilePath, ExtractImport.Data);
                    ExtractedObjectFiles.Add(OutputObjectFilePath);

                    Log.LogMessage(MessageImportance.High, "\t-> " + OutputObjectFilePath);
                }
            }

            ExtractedFiles = [.. ExtractedObjectFiles];
            return true;
        } catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true, true, Source);
            return false;
        }
    }

    private static ArchiveFile.Import FilterImports(List<ArchiveFile.Import> Imports, Predicate<ArchiveFile.Import>[] Match, Int32 MatchIndex, String ExceptionMessage)
    {
        Imports = Imports.FindAll(Match[MatchIndex]);
        if (Imports.Count == 0)
        {
            throw new ArgumentException(ExceptionMessage);
        } else if (Imports.Count == 1)
        {
            return Imports[0];
        } else
        {
            return MatchIndex + 1 < Match.Length ?
                FilterImports(Imports, Match, MatchIndex + 1, ExceptionMessage) :
                throw new ArgumentException(ExceptionMessage);
        }
    }
}
