using System;
using System.IO;
using System.Text;

// ReSharper disable EmptyGeneralCatchClause

namespace Rapid_Reporter.HTML
{
    internal static class HtmlEmbedder
    {
        internal static string BuildSessionRow_Img(int imgCount, string imgFile)
        {
            var str = MakeImageSrcData(imgFile);
            return
                string.Format(
                    "<a href=\"#\" onclick=\"ShowImgEle('imgdiv{0}', 'imgbig{0}', 'imgsmall{0}');\"><img id='imgsmall{0}' src=\"\"></a><script>var imgSrcData{0} = \"{1}\"; document.getElementById(\"imgsmall{0}\").src = imgSrcData{0};</script>{2}",
                    imgCount, str, "&nbsp;");
        }

        internal static string BuildPopUp_Img(int imgCount)
        {
            return
                string.Format(
                    "<div id='imgdiv{0}' style=\"{1}\" onclick=\"HideImgEle('imgdiv{0}')\">Click here to hide...<br><br><img id='imgbig{0}'></div>",
                    imgCount, "position: absolute; top: 40px; left: 5px; right: 0; bottom: 0; display:none;");
        }

        internal static string BuildSessionRow_PTNote(int noteCount)
        {
            return
                string.Format("<a href=\"#\" onclick=\"ShowPlaintextNote('ptndiv{0}');\">Click to show note...</a>{1}",
                    noteCount, "&nbsp;");
        }

        internal static string BuildPopUp_PTNote(int noteCount, string noteFile)
        {
            var plainTextNote = GetPlainTextNote(noteFile);
            return
                string.Format(
                    "<div id='ptndiv{0}' style=\"{1}\"><div><a href=\"#\" onclick=\"HidePlaintextNote('ptndiv{0}')\">Click here to hide...</a></div><pre>{2}</pre></div>",
                    noteCount, "position: absolute; top: 40px; left: 5px; right: 0; bottom: 0; display:none;",
                    plainTextNote);
        }

        internal static string MakeImageSrcData(string filename)
        {
            try
            {
                var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                var numArray = new byte[fileStream.Length];
                fileStream.Read(numArray, 0, Convert.ToInt32(fileStream.Length));
                var str = "data:image/png;base64," + Convert.ToBase64String(numArray, Base64FormattingOptions.None);
                fileStream.Close();
                return str;
            }
            catch
            {
                return "";
            }
        }

        private static string GetPlainTextNote(string filename)
        {
            try
            {
                var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                var numArray = new byte[fileStream.Length];
                fileStream.Read(numArray, 0, Convert.ToInt32(fileStream.Length));
                var @string = Encoding.UTF8.GetString(numArray);
                fileStream.Close();
                return @string;
            }
            catch
            {
                return "";
            }
        }
    }
}
