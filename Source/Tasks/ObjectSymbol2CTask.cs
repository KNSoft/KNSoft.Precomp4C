using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

using KNSoft.C4Lib;
using System.Xml;

namespace KNSoft.Precomp4C;

public class ObjectSymbol2CTask : Precomp4CTask
{
    [Required]
    public required String OutputHeader { get; set; }

    [Required]
    public required String OutputSource { get; set; }

    public override Boolean Execute()
    {
        try
        {

            XmlDocument doc = new();
            doc.Load(Source);

            if (doc.DocumentElement == null)
            {
                throw new InvalidDataException("Invalid XML file: " + Source);
            }

            if (doc.DocumentElement.Name != "ObjectSymbol2C")
            {
                throw new InvalidDataException("Invalid root element: " + doc.DocumentElement.Name);
            }

            // TODO
            throw new NotImplementedException();

            foreach (XmlElement ObjectFile in doc.DocumentElement.GetElementsByTagName("ObjectFile").OfType<XmlElement>())
            {
            }

            CreateCSourceOutputStreams(OutputHeader, OutputSource, out var HeaderStream, out var SourceStream);

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
