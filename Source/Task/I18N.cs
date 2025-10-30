using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Globalization;

using Microsoft.Build.Framework;

using KNSoft.C4Lib;
using KNSoft.C4Lib.CodeHelper;

namespace KNSoft.Precomp4C.Task;

public class I18N : Precomp4CTask
{
    [Required]
    public required String OutputHeader { get; set; }

    [Required]
    public required String OutputSource { get; set; }

    private class I18NLocale
    {
        public required String Name;
        public I18NLocale? Fallback = null;
        public List<String?> Strings = [];
    }

    private class I18NTable
    {
        public required String Name;
        public UInt32 FallbackLocale = 0;
        public List<I18NLocale> Locales = [];
        public List<String> StringNames = [];
    }

    private readonly List<I18NTable> Tables = [];

    private static readonly String IncludeI18N = "#include <KNSoft/Precomp4C/I18N/I18N.h>";

    public override Boolean Execute()
    {
        try
        {
            StreamWriter HeaderStream = CreateCHeaderOutputStream(OutputHeader);
            StreamWriter SourceStream = CreateCSourceOutputStream(OutputSource);

            XmlDocument doc = new();
            doc.Load(Source);

            if (doc.DocumentElement == null)
            {
                throw new InvalidDataException("Invalid XML file: " + Source);
            }

            if (doc.DocumentElement.Name != "I18N")
            {
                throw new InvalidDataException("Invalid root element: " + doc.DocumentElement.Name);
            }

            /* Covert data */

            foreach (XmlElement TableNode in doc.DocumentElement.GetElementsByTagName("Table").OfType<XmlElement>())
            {
                XmlAttributeCollection TableNodeAttr = TableNode.Attributes;
                String TableName = TableNodeAttr["Name"]?.Value ?? throw new ArgumentException("Table 'Name' unspecified in: " + TableNode.OuterXml);

                I18NTable Table = new() { Name = EscapeCSymbolName(TableName) };
                UInt32 iString = 0;

                foreach (XmlElement StringNode in TableNode.GetElementsByTagName("String").OfType<XmlElement>())
                {
                    XmlAttributeCollection StringNodeAttr = StringNode.Attributes;
                    String StringName = StringNodeAttr["Name"]?.Value ?? throw new ArgumentException("String 'Name' unspecified in: " + StringNode.OuterXml);

                    foreach (XmlElement ValueNode in StringNode.GetElementsByTagName("Value").OfType<XmlElement>())
                    {
                        XmlAttributeCollection ValueNodeAttr = ValueNode.Attributes;
                        String ValueLocale = ValueNodeAttr["Locale"]?.Value ?? throw new ArgumentException("Value 'Locale' unspecified in: " + ValueNode.OuterXml);

                        Boolean LocaleExisted = false;
                        foreach (I18NLocale Locale in Table.Locales)
                        {
                            if (Locale.Name == ValueLocale)
                            {
                                Locale.Strings.Add(ValueNode.InnerText);
                                LocaleExisted = true;
                                break;
                            }
                        }
                        if (!LocaleExisted)
                        {
                            I18NLocale NewLocale = new() { Name = ValueLocale };
                            for (UInt32 i = 0; i < iString; i++)
                            {
                                NewLocale.Strings.Add(null);
                            }
                            NewLocale.Strings.Add(ValueNode.InnerText);
                            Table.Locales.Add(NewLocale);
                        }
                    }

                    foreach (I18NLocale Locale in Table.Locales)
                    {
                        if (Locale.Strings.Count <= iString)
                        {
                            Locale.Strings.Add(null);
                        }
                    }

                    Table.StringNames.Add(StringName);
                    iString++;
                }

                /* Construct fallback list */

                String? FallbackLocale = TableNodeAttr["FallbackLocale"]?.Value;
                for (Int32 i = 0; i < Table.Locales.Count; i++)
                {
                    if (FallbackLocale != null && Table.Locales[i].Name == FallbackLocale)
                    {
                        Table.FallbackLocale = (UInt32)i;
                        FallbackLocale = null;
                    }

                    String CultureName = Table.Locales[i].Name;
                    do
                    {
                        try
                        {
                            CultureInfo Culture = new(CultureName);
                            if (String.IsNullOrEmpty(Culture.Parent.Name))
                            {
                                break;
                            }
                            CultureName = Culture.Parent.Name;
                        } catch
                        {
                            Log.LogWarning("I18N",
                                           "001",
                                           "",
                                           Source,
                                           0,
                                           0,
                                           0,
                                           0,
                                           "Locale name '" + CultureName + "' not found in System.Globalization.CultureInfo");
                            break;
                        }

                        Table.Locales[i].Fallback = Rtl.EnumerableFirstOrNull(Table.Locales, x => x.Name == CultureName) ?? Table.Locales[i].Fallback;
                    } while (true);
                }

                Tables.Add(Table);
            }

            /* Write C source */

            Cpp.OutputWithNewLine(HeaderStream, IncludeI18N);
            Cpp.OutputWithNewLine(SourceStream, IncludeI18N);
            foreach (I18NTable Table in Tables)
            {
                HeaderStream.WriteLine("enum");
                HeaderStream.WriteLine("{");
                foreach (String StringName in Table.StringNames)
                {
                    HeaderStream.WriteLine("    Precomp4C_I18N_" + Table.Name + '_' + StringName + ',');
                }
                Cpp.OutputWithNewLine(HeaderStream, "};");

                String TableDef = "PRECOMP4C_I18N_TABLE Precomp4C_I18N_Table_" + Table.Name;
                SourceStream.WriteLine(TableDef + " = {");
                SourceStream.WriteLine("    (void*)0,");
                SourceStream.WriteLine("    " + Table.FallbackLocale.ToString() + ',');
                SourceStream.WriteLine("    " + Table.Locales.Count + ',');
                SourceStream.WriteLine("    " + Table.StringNames.Count + ',');
                SourceStream.WriteLine("   {");
                for (Int32 i = 0; i < Table.Locales.Count; i++)
                {
                    I18NLocale? Fallback = Table.Locales[i].Fallback;

                    SourceStream.WriteLine("        &(PRECOMP4C_I18N_LOCALE)");
                    SourceStream.WriteLine("        {");
                    SourceStream.WriteLine("            " + (Fallback == null ? "0xFFFF" : Table.Locales.IndexOf(Fallback).ToString()) + ',');
                    SourceStream.WriteLine("            L\"" + Table.Locales[i].Name + "\",");
                    SourceStream.WriteLine("            {");
                    foreach (String? StringValue in Table.Locales[i].Strings)
                    {
                        SourceStream.WriteLine("                " + (StringValue == null ? "(void*)0" : "L\"" + StringValue + "\"") + ',');
                    }
                    SourceStream.WriteLine("            }");
                    SourceStream.WriteLine("        },");
                }
                SourceStream.WriteLine("    }");
                Cpp.OutputWithNewLine(SourceStream, "};");
                HeaderStream.WriteLine("EXTERN_C " + TableDef + ';');
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
