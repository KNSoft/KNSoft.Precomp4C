using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using Microsoft.Build.Framework;

using KNSoft.C4Lib;
using KNSoft.C4Lib.PEImage;

namespace KNSoft.Precomp4C
{
    public class DllStubTask : Precomp4CTask
    {
        [Required]
        public required String OutputFile { get; set; }

        [Required]
        public required String Platform { get; set; }

        public override Boolean Execute()
        {
            try
            {
                List<KeyValuePair<String, List<DllStub.DllExport>>> Exports = new();
                IMAGE_FILE_MACHINE Machine = FileHeader.GetMachineType(Platform);
                ParseXML(Machine, Source, Exports);
                FileStream Output = File.Open(OutputFile, FileMode.Create, FileAccess.Write);
                Rtl.WriteToStream(Output, DllStub.MakeImportStubLibraryFile(Machine, Exports));
                Output.Dispose();
                Log.LogMessage(MessageImportance.High, "\t-> " + OutputFile);
                return true;
            } catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true, true, Source);
                return false;
            }
        }

        private static void ParseXML(IMAGE_FILE_MACHINE Machine, String XmlPath, List<KeyValuePair<String, List<DllStub.DllExport>>> Exports)
        {
            XmlDocument doc = new();
            doc.Load(XmlPath);

            if (doc.DocumentElement == null)
            {
                throw new InvalidDataException("Invalid XML file: " + XmlPath);
            }

            if (doc.DocumentElement.Name != "DllStub")
            {
                throw new InvalidDataException("Invalid root element: " + doc.DocumentElement.Name);
            }

            foreach (XmlElement Dll in doc.DocumentElement.GetElementsByTagName("Dll").OfType<XmlElement>())
            {
                XmlAttributeCollection DllAttr = Dll.Attributes;
                String? DllName = (DllAttr["Name"]?.Value) ?? throw new ArgumentException("Dll 'Name' unspecified in: " + Dll.OuterXml);

                List<DllStub.DllExport> DllExportList = new();
                foreach (XmlElement DllExport in Dll.ChildNodes.OfType<XmlElement>())
                {
                    if (DllExport.Name != "Export")
                    {
                        throw new ArgumentException("Unrecognized element: " + DllExport.OuterXml);
                    }

                    XmlAttributeCollection ExportAttr = DllExport.Attributes;
                    String? ExportName = (ExportAttr["Name"]?.Value) ?? throw new ArgumentException("Export 'Name' unspecified in: " + DllExport.OuterXml);

                    String? CallConv, Archs, Arg, Type;
                    String DecoratedName;
                    IMPORT_OBJECT_NAME_TYPE NameType;
                    IMPORT_OBJECT_TYPE ObjectType;

                    /* Arch */
                    Archs = ExportAttr["Arch"]?.Value;
                    if (Archs != null && !Array.Exists(Archs.Split(' '), x => FileHeader.GetMachineType(x) == Machine))
                    {
                        continue;
                    }

                    /* CallConv */
                    if (Machine == IMAGE_FILE_MACHINE.AMD64)
                    {
                        CallConv = "__fastcall";
                    } else
                    {
                        CallConv = ExportAttr["CallConv"]?.Value;
                    }
                    if (CallConv == "__stdcall")
                    {
                        Arg = ExportAttr["Arg"]?.Value;

                        UInt32 ArgSize = 0;
                        if (Arg != null)
                        {
                            foreach (String Param in Arg.Split(' '))
                            {
                                ArgSize += Param switch
                                {
                                    "ptr" => FileHeader.GetMachineBits(Machine) / 8,
                                    "long" => 4,
                                    "int" => 4,
                                    "float" => 4,
                                    "double" => 8,
                                    _ => throw new ArgumentException("Unrecognized argument type: " + Arg + "in " + DllExport.OuterXml)
                                };
                            }
                        }

                        DecoratedName = '_' + ExportName + '@' + ArgSize.ToString();
                        NameType = IMPORT_OBJECT_NAME_TYPE.NAME_UNDECORATE;
                    } else if (CallConv == "__cdecl") {
                        DecoratedName = '_' + ExportName;
                        NameType = IMPORT_OBJECT_NAME_TYPE.NAME_UNDECORATE;
                    } else if (CallConv == "__fastcall")
                    {
                        DecoratedName = ExportName;
                        NameType = IMPORT_OBJECT_NAME_TYPE.NAME;
                    } else
                    {
                        throw new ArgumentException("Unrecognized calling convention: " + CallConv);
                    }

                    /* Type */
                    Type = ExportAttr["Type"]?.Value;
                    if (Type != null)
                    {
                        ObjectType = Type switch
                        {
                            "Data" => IMPORT_OBJECT_TYPE.DATA,
                            "Const" => IMPORT_OBJECT_TYPE.CONST,
                            "Code" => IMPORT_OBJECT_TYPE.CODE,
                            _ => throw new ArgumentException("Unrecognized 'Type' of: " + DllExport.OuterXml)
                        };
                    } else
                    {
                        ObjectType = IMPORT_OBJECT_TYPE.CODE;
                    }

                    DllExportList.Add(new(ObjectType, DecoratedName, NameType));
                }
                if (DllExportList.Count > 0)
                {
                    Exports.Add(new(DllName, DllExportList));
                }
            }
        }
    }
}
