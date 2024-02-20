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

    public static void CreateCSourceOutputStreams(String HeaderPath, String SourcePath, out FileStream HeaderStream, out FileStream SourceStream)
    {
        FileStream OutputHeaderStream = File.Open(HeaderPath, FileMode.Create, FileAccess.Write);
        FileStream OutputSourceStream = File.Open(SourcePath, FileMode.Create, FileAccess.Write);

        Rtl.StreamWrite(OutputHeaderStream, CodeFragment.AutoGenerateComment);
        Rtl.StreamWrite(OutputSourceStream, CodeFragment.AutoGenerateComment);
        Rtl.StreamWrite(OutputHeaderStream, CodeFragment.PragmaOnce);

        HeaderStream = OutputHeaderStream;
        SourceStream = OutputSourceStream;
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
