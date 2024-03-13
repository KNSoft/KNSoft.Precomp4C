using System;
using System.IO;
using System.Text;
using System.Xml;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using KNSoft.C4Lib;
using KNSoft.C4Lib.PEImage;

namespace KNSoft.Precomp4C;

public abstract class Precomp4CTask : Task
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
