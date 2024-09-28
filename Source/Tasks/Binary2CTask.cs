using System;
using System.IO;

using Microsoft.Build.Framework;

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
            FileStream HeaderStream = CreateCHeaderOutputStream(OutputHeader);
            FileStream SourceStream = CreateCSourceOutputStream(OutputSource);

            OutputCBytes(HeaderStream,
                         SourceStream,
                         "Precomp4C_Binary2C_" + Path.GetFileNameWithoutExtension(Source),
                         File.ReadAllBytes(Source));

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
