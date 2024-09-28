using System;
using System.IO;

using Microsoft.Build.Framework;

using KNSoft.C4Lib;
using System.Collections.Generic;
using System.Text;

namespace KNSoft.Precomp4C.Task;

public class Res2C : Precomp4CTask
{
    [Required]
    public required String OutputHeader { get; set; }

    [Required]
    public required String OutputSource { get; set; }

    private class DataEntry
    {
        public String? TypeName;
        public UInt16? TypeOrdinal;
        public String? TypeSymbol;
        public String? TypeDef;
        public String? Name;
        public UInt16? NameOrdinal;
        public String? NameSymbol;
        public String? NameDef;
        public UInt16 LangId;
        public String? Symbol;
    }

    private static Int32 GetNameOrOrdinal(out String? Name, out UInt16? Ordinal, out String SymbolName, out String DefString, Byte[] Data, Int32 Offset)
    {
        UInt16 Num;
        Char Ch;

        Num = BitConverter.ToUInt16(Data, Offset);
        if (Num == 0xFFFF)
        {
            Name = null;
            Ordinal = BitConverter.ToUInt16(Data, Offset + 2);
            DefString = "(const wchar_t*)(size_t)" + Ordinal.ToString();
            SymbolName = 'O' + Ordinal.ToString();
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
            DefString = "L\"" + Name + '"';
            SymbolName = 'S' + EscapeCSymbolName(Name);
            return (Name.Length + 1) * 2;
        }
    }

    private static readonly Byte[] IncludeRes2C = "#include <KNSoft/Precomp4C/Res2C/Res2C.h>\r\n\r\n"u8.ToArray();

    public override Boolean Execute()
    {
        try
        {
            List<DataEntry> Entries = [];
            FileStream HeaderStream = CreateCHeaderOutputStream(OutputHeader);
            FileStream SourceStream = CreateCSourceOutputStream(OutputSource);

            Rtl.StreamWrite(HeaderStream, IncludeRes2C);
            Rtl.StreamWrite(SourceStream, IncludeRes2C);

            Byte[] Data = File.ReadAllBytes(Source);
            UInt32 DataSize, HeaderSize;
            Int32 EntryOffset, Offset = 0;

            while (Offset < Data.Length)
            {
                EntryOffset = Offset;
                DataSize = BitConverter.ToUInt32(Data, Offset);
                Offset += 4;
                HeaderSize = BitConverter.ToUInt32(Data, Offset);
                if (DataSize != 0)
                {
                    DataEntry Entry = new();
                    Offset += 4;
                    Offset += GetNameOrOrdinal(out Entry.TypeName, out Entry.TypeOrdinal, out Entry.TypeSymbol, out Entry.TypeDef, Data, Offset);
                    Offset += GetNameOrOrdinal(out Entry.Name, out Entry.NameOrdinal, out Entry.NameSymbol, out Entry.NameDef, Data, Offset);
                    Offset += 6; // DataVersion + MemoryFlags
                    Entry.LangId = BitConverter.ToUInt16(Data, Offset);
                    Entry.Symbol = "Precomp4C_Res2C_" + EscapeCSymbolName(Path.GetFileNameWithoutExtension(Source)) + '_' +
                                   Entry.TypeSymbol + '_' + Entry.NameSymbol + '_' + Entry.LangId.ToString();
                    OutputCBytes(HeaderStream,
                                 SourceStream,
                                 Entry.Symbol,
                                 Rtl.ArraySlice(Data, EntryOffset + (Int32)HeaderSize, (Int32)DataSize));
                    Entries.Add(Entry);
                }
                Offset = EntryOffset + (Int32)HeaderSize + (Int32)DataSize;
                Offset += Offset % 4;
            }

            if (Entries.Count > 0)
            {
                String TableDef = "PRECOMP4C_RES2C_ENTRY Precomp4C_Res2C_" +
                                  EscapeCSymbolName(Path.GetFileNameWithoutExtension(Source)) + '[' + Entries.Count.ToString() + ']';
                Rtl.StreamWrite(HeaderStream, Encoding.UTF8.GetBytes("EXTERN_C " + TableDef + ';'));
                Rtl.StreamWrite(SourceStream, Encoding.UTF8.GetBytes(TableDef + " = {\r\n"));
                for (Int32 i = 0; i < Entries.Count; i++)
                {
                    Rtl.StreamWrite(SourceStream, Encoding.UTF8.GetBytes("    { " +
                                                                         Entries[i].TypeDef + ", " +
                                                                         Entries[i].NameDef + ", " +
                                                                         Entries[i].LangId.ToString() + ", " +
                                                                         Entries[i].Symbol + ", " +
                                                                         "sizeof(" + Entries[i].Symbol + ") },\r\n"));
                }
                Rtl.StreamWrite(SourceStream, Encoding.UTF8.GetBytes("};\r\n"));
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
