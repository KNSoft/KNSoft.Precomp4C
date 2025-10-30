using System;
using System.IO;
using System.Xml;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using KNSoft.C4Lib;
using KNSoft.C4Lib.PEImage;
using KNSoft.C4Lib.CodeHelper;

namespace KNSoft.Precomp4C;

public abstract class Precomp4CTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public required String Source { get; set; }

    protected static readonly UInt32 BytesPerLine = 25;

    public static StreamWriter CreateCHeaderOutputStream(String FilePath)
    {
        StreamWriter Stream = Cpp.CreateOutputFile(FilePath);
        Cpp.OutputWithNewLine(Stream, Cpp.CodeFragment.AutoGenerateFileComment);
        Cpp.OutputWithNewLine(Stream, Cpp.CodeFragment.PragmaOnce);
        return Stream;
    }

    public static StreamWriter CreateCSourceOutputStream(String FilePath)
    {
        StreamWriter Stream = Cpp.CreateOutputFile(FilePath);
        Cpp.OutputWithNewLine(Stream, Cpp.CodeFragment.AutoGenerateFileComment);
        return Stream;
    }

    public static String EscapeCSymbolName(String SymbolName)
    {
        return SymbolName.Replace('.', '_');
    }

    public static void OutputCBytes(StreamWriter HeaderStream, StreamWriter SourceStream, String SymbolName, Byte[] Data)
    {
        UInt32 RemainSize = (UInt32)Data.Length;
        String SymbolDef = String.Format("unsigned char {0}[{1:D}]", SymbolName, RemainSize);
        HeaderStream.WriteLine("EXTERN_C " + SymbolDef + ";");
        SourceStream.WriteLine(SymbolDef + " = {");
        while (RemainSize > 0)
        {
            UInt32 LineSize = Math.Min(RemainSize, BytesPerLine);
            String Line = "    ";

            for (UInt32 j = 0; j < LineSize; j++)
            {
                Line += String.Format("0x{0:X2}", Data[(UInt32)Data.Length - RemainSize--]);
                if (RemainSize > 0)
                {
                    Line += ", ";
                }
            }
            SourceStream.WriteLine(Line);
        }
        Cpp.OutputWithNewLine(SourceStream, "};");
    }

    public static Boolean FilterXmlArch(XmlAttributeCollection XmlAttr, IMAGE_FILE_MACHINE Machine)
    {
        String? Archs = XmlAttr["Arch"]?.Value;

        if (Archs != null && !Array.Exists(Archs.Split(' '), x => FileHeader.GetMachineType(x) == Machine))
        {
            return true;
        }

        return false;
    }
}

static class Program
{
    static int Main()
    {
        return 0;
    }
}
