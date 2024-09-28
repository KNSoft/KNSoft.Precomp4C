using System;
using System.IO;

using Microsoft.Build.Framework;

using KNSoft.C4Lib;

namespace KNSoft.Precomp4C.Task;

public class Res2C : Precomp4CTask
{
    [Required]
    public required String OutputHeader { get; set; }

    [Required]
    public required String OutputSource { get; set; }

    private static Int32 GetNameOrOrdinal(out String? Name, out UInt16? Ordinal, Byte[] Data, Int32 Offset)
    {
        UInt16 Num;
        Char Ch;

        Num = BitConverter.ToUInt16(Data, Offset);
        if (Num == 0xFFFF)
        {
            Name = null;
            Ordinal = BitConverter.ToUInt16(Data, Offset + 2);
            return 4;
        } else
        {
            Ch = Convert.ToChar(Num);
            Name = String.Empty;
            while (Ch != '\0')
            {
                Name += Ch;
                Offset += 2;
                Ch = BitConverter.ToChar(Data, Offset);
            }
            Ordinal = null;
            return (Name.Length + 1) * 2;
        }
    }

    public override Boolean Execute()
    {
        try
        {
            FileStream HeaderStream = CreateCHeaderOutputStream(OutputHeader);
            FileStream SourceStream = CreateCSourceOutputStream(OutputSource);

            Byte[] Data = File.ReadAllBytes(Source);
            UInt32 DataSize, HeaderSize;
            UInt16 LangId;
            UInt16? Ordinal;
            Int32 EntryOffset, Offset = 0;

            while (Offset < Data.Length)
            {
                EntryOffset = Offset;
                DataSize = BitConverter.ToUInt32(Data, Offset);
                Offset += 4;
                HeaderSize = BitConverter.ToUInt32(Data, Offset);
                if (DataSize != 0)
                {
                    Offset += 4;
                    Offset += GetNameOrOrdinal(out var Type, out Ordinal, Data, Offset);
                    Type ??= Ordinal.ToString();
                    Offset += GetNameOrOrdinal(out var Name, out Ordinal, Data, Offset);
                    Name ??= Ordinal.ToString();
                    Offset += 6; // DataVersion + MemoryFlags
                    LangId = BitConverter.ToUInt16(Data, Offset);
                    OutputCBytes(HeaderStream,
                                 SourceStream,
                                 "Precomp4C_Res2C_" + Type + "_" + Name + "_" + LangId.ToString(),
                                 Rtl.ArraySlice(Data, EntryOffset + (Int32)HeaderSize, (Int32)DataSize));
                }
                Offset = EntryOffset + (Int32)HeaderSize + (Int32)DataSize;
                Offset += Offset % 4;
            }

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
