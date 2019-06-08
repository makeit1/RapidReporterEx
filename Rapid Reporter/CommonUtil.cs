using System;

namespace Rapid_Reporter
{
    internal static class CommonUtil
    {
        //extension must have a dot like ".txt"
        internal static string GenerateFilename(string workingDir, string extension)
        {
            string filename;
            do
            {
                filename = DateTime.Now.ToString("yyyyMMdd_HHmmss") + extension;
            } while (System.IO.File.Exists(workingDir + filename));
            return filename;
        }
    }
}
