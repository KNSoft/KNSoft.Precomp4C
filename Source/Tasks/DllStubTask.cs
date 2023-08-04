using System;
using System.IO;
using System.Xml;
using System.Linq;
using Microsoft.Build.Framework;

using KNSoft.C4Lib.PEImage;

namespace KNSoft.Precomp4C;

public class DllStubTask : Precomp4CTask
{
    [Required]
    public required String[] SearchPaths { get; set; }

    [Required]
    public required String OutputFile { get; set; }

    [Required]
    public required String Platform { get; set; }

    public override Boolean Execute()
    {
        try
        {
            FileStream Output = File.Open(OutputFile, FileMode.Create, FileAccess.Write);
            ParseXMLToAr(FileHeader.GetMachineType(Platform), Source).Write(Output);
            Output.Dispose();

            Log.LogMessage(MessageImportance.High, "\t-> " + OutputFile);
            return true;
        } catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true, true, Source);
            return false;
        }
    }

    private static ArchiveFile ParseXMLToAr(IMAGE_FILE_MACHINE Machine, String XmlPath)
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

        ArchiveFile Ar = new(Machine);
        Ar.AddImport("Precomp4C", ObjectFile.NewNullIIDObject(Machine));

        foreach (XmlElement Dll in doc.DocumentElement.GetElementsByTagName("Dll").OfType<XmlElement>())
        {
            XmlAttributeCollection DllAttr = Dll.Attributes;
            String? DllName = (DllAttr["Name"]?.Value) ?? throw new ArgumentException("Dll 'Name' unspecified in: " + Dll.OuterXml);

            Ar.AddImport(DllName, ObjectFile.NewDllImportStubObject(Machine, DllName));

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
                                "ptr" => FileHeader.GetSizeOfPointer(Machine),
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

                Ar.AddImport(ObjectType, NameType, DllName, DecoratedName);
            }
        }

        return Ar;
    }
}
