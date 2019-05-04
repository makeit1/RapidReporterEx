using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RapidReporterSharePointFix30
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnChoosefile_Click(object sender, EventArgs e)
        {
            string text;
            var result = openFileDialog1.ShowDialog();
            if (result != DialogResult.OK) return;
            var fn = openFileDialog1.FileName;
            try
            {
                text = File.ReadAllText(fn);
            }
            catch (IOException exception)
            {
                MessageBox.Show(exception.Message, exception.ToString());
                return;
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(@"File was empty or could not be read;");
                return;
            }
            if (text.Contains("<!--RRExV"))
            {
                MessageBox.Show(@"File created with new version of Rapid Reporter Ex and cannot be converted at this time");
                return;
            }
            if (text.Contains("function ShowImgEle(eleId, img64)"))
            {
                MessageBox.Show(@"File created with really old version of Rapid Reporter Ex and cannot be converted at this time");
                return;
            }
            if (!text.Contains("function ShowImgEle(eleId, bigImgId, littleImgId)"))
            {
                MessageBox.Show(@"File created with unknown version of Rapid Reporter Ex and cannot be converted at this time");
                return;
            }
            int count;
            var outputHtml = ConvertOldStringToNew(text, out count);
            MessageBox.Show(string.Format("Successfully converted {0} images.", count));
            saveFileDialog1.FileName = openFileDialog1.FileName;
            var saveResult = saveFileDialog1.ShowDialog();
            if (saveResult != DialogResult.OK) return;
            var sfn = saveFileDialog1.FileName;
            try
            {
                File.WriteAllText(sfn, outputHtml);
            }
            catch (IOException exception)
            {
                MessageBox.Show(exception.Message, exception.ToString());
            }
        }

        private void FolderBtn_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog();
            if (result != DialogResult.OK) return;
            var files = Directory.GetFiles(folderBrowserDialog1.SelectedPath).ToList();
            files = files.Where(f => f.EndsWith("html") || f.EndsWith("htm")).ToList();
            if (!files.Any())
            {
                MessageBox.Show(@"No HTML or HTM files found in directory");
                return;
            }
            var root = folderBrowserDialog1.SelectedPath;
            if (!root.EndsWith("\\")) root += "\\";
            var errorDir = root + "error\\";
            var oldDir = root + "old\\";
            var newDir = root + "new\\";

            int errorCnt = 0, goodCnt = 0, imgCnt = 0;

            try
            {
                Directory.CreateDirectory(newDir);
                Directory.CreateDirectory(oldDir);
                Directory.CreateDirectory(errorDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.ToString());
                return;
            }

            foreach (var f in files)
            {
                string text;
                try
                {
                    text = File.ReadAllText(f);
                }
                catch (IOException exception)
                {
                    File.Move(f, errorDir + Path.GetFileName(f));
                    errorCnt++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(text))
                {
                    File.Move(f, errorDir + Path.GetFileName(f));
                    errorCnt++;
                    continue;
                }
                if (text.Contains("<!--RRExV"))
                {
                    File.Move(f, errorDir + Path.GetFileName(f));
                    errorCnt++;
                    continue;
                }
                if (text.Contains("function ShowImgEle(eleId, img64)"))
                {
                    File.Move(f, errorDir + Path.GetFileName(f));
                    errorCnt++;
                    continue;
                }
                if (!text.Contains("function ShowImgEle(eleId, bigImgId, littleImgId)"))
                {
                    File.Move(f, errorDir + Path.GetFileName(f));
                    errorCnt++;
                    continue;
                }
                int count;
                var outputHtml = ConvertOldStringToNew(text, out count);
                imgCnt += count;
                goodCnt++;
                File.WriteAllText(newDir + Path.GetFileName(f), outputHtml);
                File.Move(f, oldDir + Path.GetFileName(f));
            }
            MessageBox.Show(string.Format("Successfully converted {0} images in {1} files. {2} files failed.", imgCnt, goodCnt, errorCnt));
        }


        private static string ConvertOldStringToNew(string old, out int count)
        {
            var trimmedHtml = old;
            var outputHtml = old;
            count = 0;
            while (true)
            {
                var id = GetImageId(trimmedHtml);
                if (string.IsNullOrWhiteSpace(id)) break;
                var src = GetImageSrc(trimmedHtml, id);
                if (string.IsNullOrWhiteSpace(src)) break;
                outputHtml = GetNewOutput(outputHtml, id, src);
                trimmedHtml = TrimBuffer(trimmedHtml, id, src);
                count++;
            }
            return outputHtml;
        }

        private static string GetImageId(string msg)
        {
            var startOf1 = msg.IndexOf("');\"><img id='imgsmall", StringComparison.Ordinal);
            if (startOf1 < 0) return null;
            startOf1 += "');\"><img id='imgsmall".Length;
            var chopped = msg.Substring(startOf1);
            var startOf2 = chopped.IndexOf("' src=\"data:image/png;base64", StringComparison.Ordinal);
            if (startOf2 < 0) return null;
            return chopped.Substring(0, startOf2);
        }

        private static string GetImageSrc(string msg, string id)
        {
            var start1Str = string.Format("');\"><img id='imgsmall{0}' src=\"", id);
            var startOf1 = msg.IndexOf(start1Str, StringComparison.Ordinal);
            if (startOf1 < 0) return null;
            startOf1 += start1Str.Length;
            var chopped = msg.Substring(startOf1);
            var startOf2 = chopped.IndexOf("\"></a>&nbsp;", StringComparison.Ordinal);
            if (startOf2 < 0) return null;
            return chopped.Substring(0, startOf2);
        }

        private static string GetNewOutput(string msg, string id, string src)
        {
            var oldStr = string.Format("');\"><img id='imgsmall{0}' src=\"{1}\"></a>&nbsp;", id, src);
            var newStr = string.Format("');\"><img id='imgsmall{0}' src=\"\"></a><script>var imgSrcData{0} = \"{1}\"; document.getElementById(\"imgsmall{0}\").src = imgSrcData{0};</script>&nbsp;", id, src);
            return msg.Replace(oldStr, newStr);
        }

        private static string TrimBuffer(string msg, string id, string src)
        {
            var oldStr = string.Format("');\"><img id='imgsmall{0}' src=\"{1}\"></a>&nbsp;", id, src);
            var start = msg.IndexOf(oldStr, StringComparison.Ordinal) + oldStr.Length;
            return msg.Substring(start);
        }
    }
}
