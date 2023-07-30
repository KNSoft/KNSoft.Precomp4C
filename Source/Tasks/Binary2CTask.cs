using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;

using KNSoft.C4Lib;

namespace KNSoft.Precomp4C;

public class Binary2CTask : Precomp4CTask
{
    [Required]
    public required String OutputHeader { get; set; }

    [Required]
    public required String OutputSource { get; set; }

    public override Boolean Execute()
    {
        try
        {
            CreateCSourceOutputStreams(OutputHeader, OutputSource, out var HeaderStream, out var SourceStream);

            Byte[] Data = File.ReadAllBytes(Source);
            UInt32 RemainSize = (UInt32)Data.Length;
            String SymbolDef = String.Format("unsigned char {0}[{1:D}]", "Precomp4C_Binary2C_" + Path.GetFileNameWithoutExtension(Source), RemainSize);
            Rtl.WriteToStream(HeaderStream, Encoding.UTF8.GetBytes("extern " + SymbolDef + ";\r\n"));
            Rtl.WriteToStream(SourceStream, Encoding.UTF8.GetBytes(SymbolDef + " = {\r\n"));
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
                Rtl.WriteToStream(SourceStream, Encoding.UTF8.GetBytes(Line + "\r\n"));
            }
            Rtl.WriteToStream(SourceStream, "};\r\n"u8.ToArray());

            HeaderStream.Dispose();
            SourceStream.Dispose();

            Log.LogMessage(MessageImportance.High, "\t-> " + OutputHeader);
            Log.LogMessage(MessageImportance.High, "\t-> " + OutputSource);
            return true;
        } catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true, true, Source);
            return false;
        }
    }
}
