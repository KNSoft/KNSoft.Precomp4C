﻿using System;
using System.IO;
using System.Text;
using System.Xml;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using KNSoft.C4Lib;
using KNSoft.C4Lib.PEImage;

namespace KNSoft.Precomp4C
{
    public static class CodeFragments
    {
        public static readonly Byte[] AutoGenerateComment = Encoding.UTF8.GetBytes(
            "//------------------------------------------------------------------------------\r\n" +
            "// <auto-generated>\r\n" +
            "//     This code was generated by " + MetaInfo.ProductName + "\r\n" +
            "//     " + MetaInfo.CompanyName + " - " + MetaInfo.ProductName + "\r\n" +
            "//     " + MetaInfo.RepositoryURL + "\r\n" +
            "//     Do not change this file manually\r\n" +
            "// </auto-generated>\r\n" +
            "//------------------------------------------------------------------------------\r\n\r\n");

        public static readonly Byte[] PragmaOnce = "#pragma once\r\n\r\n"u8.ToArray();

        public static readonly Byte[] DefineExternC =
"""
#ifdef __cplusplus
#define EXTERN_C extern \"C\"
#else
#define EXTERN_C extern
#endif

"""u8.ToArray();

        public static readonly Byte[] DefineWcharT =
"""
#ifndef _WCHAR_T_DEFINED
#define _WCHAR_T_DEFINED
typedef unsigned short wchar_t;
#endif
"""u8.ToArray();
    }

    public abstract class Precomp4CTask : Task
    {
        [Required]
        public required String Source { get; set; }

        protected static readonly UInt32 BytesPerLine = 25;

        public static void CreateCSourceOutputStreams(String HeaderPath, String SourcePath, out FileStream HeaderStream, out FileStream SourceStream)
        {
            FileStream OutputHeaderStream = File.Open(HeaderPath, FileMode.Create, FileAccess.Write);
            FileStream OutputSourceStream = File.Open(SourcePath, FileMode.Create, FileAccess.Write);

            Rtl.StreamWrite(OutputHeaderStream, CodeFragments.AutoGenerateComment);
            Rtl.StreamWrite(OutputSourceStream, CodeFragments.AutoGenerateComment);
            Rtl.StreamWrite(OutputHeaderStream, CodeFragments.PragmaOnce);

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
}
