using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Rapid_Reporter.HTML;
using MessageBox = System.Windows.MessageBox;

// ReSharper disable EmptyGeneralCatchClause

namespace Rapid_Reporter
{
    class Session
    {
        /** Variables **/
        /***************/
        
        // This is configurable from inside the application:
        // Session characteristics:
        public DateTime StartingTime;   // Time started, starts when moving from 'charter' to 'notes'.
        public int Duration = 90 * 60;  // Duration, in seconds (default is 90 min, can be changed in runtime).
        private const string ColumnHeaders = "Time,Type,Content"; // Consider adding sequencial number?

        // Session data:
        public string ScenarioId = "";     // Session objective. Configured in runtime.
        public string Tester = "";      // Tester's name. Configured in runtime.
        public string Charter = "";      // Configured in runtime.
        public string Environment = "";      // Configured in runtime.
        public string Versions = "";      // Configured in runtime.
        // The types of comments. This can be overriden from command line, so every person can use his own terminology or language
        public string[] NoteTypes = { "Prerequisite", "Test", "Success", "Bug/Issue", "Note", "Follow Up", "Summary" };

        // Session files:
        public string WorkingDir = Directory.GetCurrentDirectory() + @"\";  // Directory to write the session to
        private string _sessionFile;      // File to write the session to
        private string _sessionFileFull;  // workingDir + sessionFile
        public string SessionNote = "";         // Latest note only

        // Session State Based Behavior:
        //  The application iterates: tester, charter, notes.
        //  This is done in this way in case we have to add more stages... But the stages are not moved by  number or placement, they're chosen directly.
        public enum SessionStartingStage { Tester, ScenarioId, Charter, Environment, Versions, Notes }; // Tester == tester's name. Charter == session charter. Notes == all the notes of different note types.
        public SessionStartingStage CurrentStage = SessionStartingStage.Tester; // This is used only in the beginning, in order to receive the tester name and charter text

        /** Sessions **/
        /**************/

        // Start Session and Close Session prepare/finalize the log file
        public void StartSession()
        {
            Logger.Record("[StartSession]: Session configuration starting", "Session", "info");

            StartingTime = DateTime.Now; // The time the session started is used for many things, like knowing the session file name
            WorkingDir = Directory.GetCurrentDirectory() + @"\" + StartingTime.ToString("yyyyMMdd_HHmmss") + @"\";
            _sessionFile = StartingTime.ToString("yyyyMMdd_HHmmss") + ".csv";
            _sessionFileFull = WorkingDir + _sessionFile; // All files should be written to a working directory -- be it current or not.
            CreateWorkingDir(WorkingDir);
            SaveToSessionNotes(ColumnHeaders + "\n"); // Headers of the notes table
            //UpdateNotes("Reporter Tool Version", System.Windows.Forms.Application.ProductVersion);
            UpdateNotes("Session Reporter", Tester);
            UpdateNotes("Scenario ID", ScenarioId);
            UpdateNotes("Session Charter", Charter);
            UpdateNotes("Environment", Environment);
            UpdateNotes("Versions", Versions);
        }
        
        private void CreateWorkingDir(string path)
        {
            if (Directory.Exists(path))
            {
                throw new InvalidDirecotoryException("A folder " + path + " already exits.");
            }

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception)
            {
                throw new InvalidDirecotoryException("A folder " + path + " could not be created.");
            }
        }

        internal bool ResumeSession()
        {
            Logger.Record("[ResumeSession]: Session configuration starting", "Session", "info");

            var csvFile = SelectSessionCsvForOpen();
            if (string.IsNullOrWhiteSpace(csvFile)) return false;
            LoadCsvIntoSession(csvFile);
            if (string.IsNullOrWhiteSpace(Tester) || string.IsNullOrWhiteSpace(Charter) ||
                string.IsNullOrWhiteSpace(Versions) || string.IsNullOrWhiteSpace(Environment) ||
                string.IsNullOrWhiteSpace(ScenarioId)) return false;
            _sessionFile = Path.GetFileName(csvFile);
            WorkingDir = Path.GetDirectoryName(csvFile) + @"\";
            _sessionFileFull = csvFile; 
            return true;
        }

        public void CloseSession() // Not closing directly, we first finalize the session
        {
            Logger.Record("[CloseSession]: Session closing...", "Session", "info");

            // Why this if? We will only add the 'end session' note if we were past the charter step.
            if (!String.Equals(Versions, ""))
            {
                TimeSpan duration = DateTime.Now - StartingTime;
                UpdateNotes("Session End. Duration",
                            duration.Hours.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" +
                            duration.Minutes.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" +
                            duration.Seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'));
                Logger.Record("[CloseSession]: Starting csv to html method...", "Session", "info");
                Csv2Html(_sessionFileFull, false);
            }

            Logger.Record("[CloseSession]: ...Session closed", "Session", "info");
        }

        /** Notes **/
        /***********/
        // Notes are always saved on file, not only when program exists (so no data loss in case of crash)

        // UpdateNotes: There are two overloads: One receives all strings (custom messages), the other an int (typed messages)
        internal void UpdateNotes(int type, string note, string screenshot, string rtfNote)
        {
            UpdateNotes(NoteTypes[type], note, screenshot, rtfNote);
            Logger.Record("[UpdateNotes isss]: Note added to session log. Attachments: (" + (screenshot.Length > 0) + " | " + (rtfNote.Length > 0) + ")", "Session", "info");
        }
        internal void UpdateNotes(string type, string note, string screenshot = "", string rtfNote = "")
        {
            SessionNote = DateTime.Now + "," + type + ",\"" + note + "\"," + rtfNote + "\n";
            SaveToSessionNotes(SessionNote);
            Logger.Record("[UpdateNotes ss]: Note added to session log (" + screenshot + ", " + rtfNote + ")", "Session", "info");
        }
        // Save all notes on file, after every single note
        private void SaveToSessionNotes(string note)
        {
            Logger.Record("[SaveToSessionNotes]: File will be updated and saved to " + _sessionFile, "Session", "info");
            bool exDrRetry;

            do
            { exDrRetry = false;
                try
                {
                    File.AppendAllText(_sessionFileFull, note, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Logger.Record("\t[SaveToSessionNotes]: EXCEPTION reached - Session Note file could not be saved (" + _sessionFile + ")", "Session", "error");
                    exDrRetry = Logger.FileErrorMessage(ex, "SaveToSessionNotes", _sessionFile);
                }
            } while (exDrRetry);
        }

        private string DiscoverSavePath(string csvFile)
        {
            var str1 =
                (new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())).Aggregate(
                    string.Format("{0} - {1}.htm", Path.GetFileNameWithoutExtension(csvFile), ScenarioId),
                    (current, c) => current.Replace(c.ToString(CultureInfo.InvariantCulture), ""));
            var str2 = WorkingDir + str1;
            var saveFileDialog1 = new SaveFileDialog
            {
                DefaultExt = "htm",
                FileName = str1,
                InitialDirectory = WorkingDir
            };
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                str2 = saveFileDialog1.FileName;
            return str2;
        }

        private string SelectSessionCsvForOpen()
        {
            var openFileDialog = new OpenFileDialog()
            {
                DefaultExt = "csv",
                InitialDirectory = WorkingDir
            };
            return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : "";
        }

        private static string BuildTableRow(string rowType, string entryType, string timestamp, string value)
        {
            return
                string.Format(
                    "<tr class=\"{0}\"> <{1} class=\"timestamp\">{2}</{1}><{1} class=\"notetype\">{0}</{1}><{1}>{3}</{1}></tr>\n",
                    entryType, rowType, timestamp, value);
        }

        public void Csv2Html(string csvFile, bool relativePath)
        {
            Logger.Record("[CSV2HTML]: HTML Report building", "Session", "info");
            var csvFileFull = relativePath ? WorkingDir + csvFile : csvFile;
            var htmlFileFull = DiscoverSavePath(csvFile);
            bool exDrRetry;

            do
            {
                exDrRetry = false;
                var htmlFileBufferPopups = "";
                try
                {
                    var imgCount = 0;
                    var ptnCount = 0;
                    var t = "th";
                    var title = string.Format("{0}{1}", ScenarioId, Htmlstrings.HtmlTitle);
                    File.Delete(htmlFileFull);
                    var htmlTop = string.Format("{0}{1}{2}{3}{4}{5}{1}{6}", (object)Htmlstrings.AHtmlHead,
                        (object)title, (object)Htmlstrings.BTitleOut, (object)Htmlstrings.CStyle,
                        (object) Htmlstrings.DJavascript, (object) Htmlstrings.EBody, (object) Htmlstrings.GTable);
                    var topNotes = "";
                    var bottomNotes = "";

                    foreach (var line in File.ReadAllLines(csvFileFull, Encoding.UTF8))
					{
                        if ("" == line) continue;
                        var note = ""; 
                        var thisLine = line.Split(',');
                        if (thisLine.Length > 2)
                        {
                            note = thisLine[2].Replace("\"", "");
                            switch (thisLine[1])
                            {
                                case @"Screenshot":
                                    if (!File.Exists(WorkingDir + note))
                                    {
                                        note += " not found.";
                                        break;
                                    }
                                    note = HtmlEmbedder.BuildSessionRow_Img(imgCount, WorkingDir + note);
                                    htmlFileBufferPopups += HtmlEmbedder.BuildPopUp_Img(imgCount);
                                    imgCount++;
                                    break;
                                case @"PlainText Note":
                                    if (!File.Exists(WorkingDir + note))
                                    {
                                        note += " not found.";
                                        break;
                                    }
                                    htmlFileBufferPopups += HtmlEmbedder.BuildPopUp_PTNote(ptnCount, WorkingDir + note);
                                    note = HtmlEmbedder.BuildSessionRow_PTNote(ptnCount);
                                    ptnCount++;
                                    break;
                            }
                        }

					    if (thisLine[1] == "Type" || thisLine[1] == "Session Reporter" ||
					        (thisLine[1] == "Scenario ID" || thisLine[1] == "Session Charter") ||
					        (thisLine[1] == "Environment" || thisLine[1] == "Versions" || thisLine[1] == "Summary"))
					    {
					        topNotes += BuildTableRow(t, thisLine[1], thisLine[0], note);
					    }
					    else
					    {
					        bottomNotes += BuildTableRow(t, thisLine[1], thisLine[0], note);
					    }
                        t = "td";
                    }
                    topNotes = topNotes + BuildTableRow("td", "", "", "");
                    var output = htmlTop +
                                 string.Format("{0}{1}{2}{3}{4}", topNotes, bottomNotes,
                                     Htmlstrings.JTableEnd, htmlFileBufferPopups, Htmlstrings.MHtmlEnd);

                    File.WriteAllText(htmlFileFull, output, Encoding.UTF8);
                    MessageBox.Show("The HTML was created successfully!\nFile created: " + htmlFileFull, "HTML Conversion Successful!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.Record("[CSV2HTML]: EXCEPTION reached - Session Report file could not be saved (" + htmlFileFull + ")", "Session", "error");
                    exDrRetry = Logger.FileErrorMessage(ex, "CSV to HTML", htmlFileFull);
                }
            } while (exDrRetry);
            Logger.Record("[CSV2HTML]: HTML Report built, done.", "Session", "info");
        }

        private void LoadCsvIntoSession(string csvFile)
        {
            Logger.Record("[LoadCsvIntoSession]: Grabbing CSV file variables", "Session", "info");

            bool exDrRetry;
            do
            {
                exDrRetry = false;
                try
                {
                    foreach (var line in File.ReadAllLines(csvFile, Encoding.UTF8))
                    {
                        if ("" == line) continue;
                        var thisLine = line.Split(',');
                        if (thisLine.Length <= 2) continue;
                        var note = thisLine[2].Replace("\"", "");
                        switch (thisLine[1])
                        {
                            case @"Session Reporter":
                                Tester = note;
                                StartingTime = DateTime.Parse(thisLine[0]);
                                break;
                            case @"Scenario ID":
                                ScenarioId = note;
                                break;
                            case @"Session Charter":
                                Charter = note;
                                break;
                            case @"Environment":
                                Environment = note;
                                break;
                            case @"Versions":
                                Versions = note;
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Record("[LoadCsvIntoSession]: EXCEPTION reached - Session Report file could not be read (" + csvFile + ")", "Session", "error");
                    exDrRetry = Logger.FileErrorMessage(ex, "LoadCsvIntoSession", csvFile);
                }
            } while (exDrRetry);
            Logger.Record("[LoadCsvIntoSession]: Grabbing CSV file variables done.", "Session", "info");
        }
    }
}
