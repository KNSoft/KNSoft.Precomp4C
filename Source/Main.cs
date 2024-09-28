using System;
using System.IO;
using System.Xml;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using KNSoft.C4Lib;
using KNSoft.C4Lib.PEImage;

namespace KNSoft.Precomp4C;

public abstract class Precomp4CTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public required String Source { get; set; }

    protected static readonly UInt32 BytesPerLine = 25;

    public static FileStream CreateCHeaderOutputStream(String FilePath)
    {
        FileStream Stream = File.Open(FilePath, FileMode.Create, FileAccess.Write);
        Rtl.StreamWrite(Stream, CodeFragment.AutoGenerateComment);
        Rtl.StreamWrite(Stream, CodeFragment.PragmaOnce);
        return Stream;
    }

    public static FileStream CreateCSourceOutputStream(String FilePath)
    {
        FileStream Stream = File.Open(FilePath, FileMode.Create, FileAccess.Write);
        Rtl.StreamWrite(Stream, CodeFragment.AutoGenerateComment);
        return Stream;
    }

    public static void OutputCBytes(FileStream HeaderStream, FileStream SourceStream, String SymbolName, Byte[] Data)
    {
        UInt32 RemainSize = (UInt32)Data.Length;
        String SymbolDef = String.Format("unsigned char {0}[{1:D}]", SymbolName, RemainSize);
        Rtl.StreamWrite(HeaderStream, Encoding.UTF8.GetBytes("extern " + SymbolDef + ";\r\n"));
        Rtl.StreamWrite(SourceStream, Encoding.UTF8.GetBytes(SymbolDef + " = {\r\n"));
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
            Rtl.StreamWrite(SourceStream, Encoding.UTF8.GetBytes(Line + "\r\n"));
        }
        Rtl.StreamWrite(SourceStream, "};\r\n"u8.ToArray());
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
