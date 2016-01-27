// This application lets the tester be the master of the session.
//  PlainTextNote - use for basic notetaking and copying xml etc

// References and Dependencies

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Effects;

#pragma warning disable 612,618
// ReSharper disable LocalizableElement

// Scope of the application.
// In this file we deal with the 'RTF Note Widget':
/*
 * +------------------------------------------------------------------------------------+
 * | +--------------------------------------------------------------------------------+ |
 * | | *ABC* aBc /123/                                                                | |
 * | +--------------------------------------------------------------------------------+ |
 * |                                                                       [ Save Note] |
 * +------------------------------------------------------------------------------------+
 */
namespace Rapid_Reporter.Forms
{
    // RTFNote controls the little text area to write enhanced notes for the session notes.
    public partial class PlainTextNote
    {
        public Boolean ForceClose = false;  // We keep the window open (although hidden) until the app is closed.
        int _currentPlainTextNote = 1;             // The number of the notes helps putting them in order, and finding them between the files (timestamp alone may confuse people).
        public SmWidget Sm;                 // Our interface to toggle the visual of the button in the main window is by direct referencing.
        public string WorkingDir = Directory.GetCurrentDirectory() + @"\";      // The directory to save files

        // Initialization, and setting focus to the richtextnote area
        public PlainTextNote()
        {
            Logger.Record("[PlainTextNote]: PlainText Note window initializing", "PlainTextNote", "info");
            InitializeComponent();
            // With the code below we control when to close the window or hide it.
            //  Didn't use a normal 'EventHandler' because we needed the Cancel option to close the window when appropriate.
            Closing += PlainTextNoteDialog_Close;
            Logger.Record("[PlainTextNote]: PlainText Note window initialized, including CancelEventHandler", "PlainTextNote", "info");
        }
        private void PlainTextNoteDialog_VisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Logger.Record("[PlainTextNoteDialog_VisibilityChanged]: PlainText Note window visibility changed", "PlainTextNote", "info");
            PlainTextNoteDialog_GotFocus();
        }
        private void PlainTextNoteDialog_GotFocus()
        {
            Logger.Record("[PlainTextNoteDialog_GotFocus]: PlainText Note window got focus", "PlainTextNote", "info");
            plainTextNote.Focus();
            plainTextNote.ScrollToEnd(); // Ready to enter new information
        }

        // When a note is cleared, there is no undo!
        private void clean_Click(object sender, RoutedEventArgs e)
        {
            Logger.Record("[clean_Click]: PlainText Note cleared", "PlainTextNote", "info");
            plainTextNote.Clear();
            PlainTextNoteDialog_GotFocus(); // after we close the button, we're ready to input notes again
        }

        // When the note is 'saved', it get's saved into an PlainText file, and marked for being attached to a session note.
        private void save_Click(object sender, RoutedEventArgs e)
        {
            Logger.Record("[save_Click]: PlainText Note to be saved", "PlainTextNote", "info");
            var textBoxContents = plainTextNote.Text;

            if (!string.IsNullOrWhiteSpace(textBoxContents)) 
            {
                Logger.Record("\t[save_Click]: PlainText Note not empty, will save", "PlainTextNote", "info");
                // Name the note, save to file
                Sm.PlainTextNoteName = _currentPlainTextNote++.ToString(CultureInfo.InvariantCulture) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                bool exDrRetry;
                do
                {
                    exDrRetry = false;
                    try
                    {
                        // Process is:
                        //  1. Save the file
                        //  2. Change the visual cue
                        //  3. Add an autogenerated line to the session CSV

                        var plainText = System.Net.WebUtility.HtmlEncode(textBoxContents);

                        // Saves the file
                        File.WriteAllText(WorkingDir + Sm.PlainTextNoteName, plainText);

                        // Set the visual effect to clue the tester there's a note attached
                        var effect = new BevelBitmapEffect {BevelWidth = 2, EdgeProfile = EdgeProfile.BulgedUp};
                        Sm.RTFNoteBtn.BitmapEffect = effect;

                        // Adds an 'autogenerated' line to the session CSV
                        Sm.SavePlainTextNote(Sm.PlainTextNoteName);
                        Logger.Record("\t\t[save_Click]: PlainText Note saved: " + Sm.PlainTextNoteName, "PlainTextNote", "info");
                        plainTextNote.Clear();
                        Logger.Record("[save_Click]: PlainText Note cleared", "PlainTextNote", "info");
                    }
                    catch (Exception ex)
                    {
                        Logger.Record("\t\t[save_Click]: EXCEPTION reached - PlainText Note file could not be saved (" + Sm.PlainTextNoteName + ")", "PlainTextNote", "error");
                        exDrRetry = Logger.FileErrorMessage(ex, "save_Click", Sm.PlainTextNoteName);
                    }
                } while (exDrRetry);
            }
            Logger.Record("[save_Click]: PlainText Note saving mechanism done. Will close (hide).", "PlainTextNote", "info");
            // We not really 'close' the window. Close function deals with whether hiding or closing it.
            Close();
            Logger.Record("[save_Click]: PlainText Note saving mechanism done. Closed (hidden).", "PlainTextNote", "info");
        }

        // Always hide the window. Unless the app is being closed completely, then close too.
        //  When hiding the window, toggle the button status on the main Widget.
        private void PlainTextNoteDialog_Close(object sender, CancelEventArgs e)
        {
            Logger.Record("[PlainTextNoteDialog_Close]: Close function called with force flag = " + ForceClose.ToString(), "PlainTextNote", "info");

            Logger.Record("[PlainTextNoteDialog_Close]: Toggling button state", "PlainTextNote", "info");
            Sm.IsPlainTextDiagOpen = false;

            // The only way I found to control on how to cancel the closing or not, was by using an external flag.
            if (ForceClose != true)
            {
                // Apparently, there is an exception in some machines when closing the app and/or PlainText
                //  we try to catch it here.
                //  Bug report: [94adb64c91]
                try
                {
                    Logger.Record("\t[PlainTextNoteDialog_Close]: Hiding PlainText Note window", "PlainTextNote", "info");
                    Logger.Record("\t[PlainTextNoteDialog_Close]: Hiding PlainText Note window - Step 1: _isClosing = false...", "PlainTextNote", "info");
                    typeof(Window).GetField("_isClosing", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, false);
                    Logger.Record("\t[PlainTextNoteDialog_Close]: Hiding PlainText Note window - Step 2: e.Cancel = true...", "PlainTextNote", "info");
                    e.Cancel = true; // Abort the close event
                    Logger.Record("\t[PlainTextNoteDialog_Close]: Hiding PlainText Note window - Step 3: this.Hide...", "PlainTextNote", "info");
                    Hide(); // Hide is throwing an exception:
                    // "Cannot set visibility or call show, showdialog, close, or hide while window is closing"
                    //  Step 1 is supposed to overcome this exception. Hopefully.

                    Logger.Record("\t[PlainTextNoteDialog_Close]: Hiding PlainText Note window - Step 3: this.Hide finished", "PlainTextNote", "info");
                }
                catch (Exception ex)
                {
                    Logger.Record("\t[PlainTextNoteDialog_Close]: EXCEPTION reached - PlainText Note closing could not be cancelled", "PlainTextNote", "error");
                    System.Windows.Forms.MessageBox.Show(
                        "Hi! An error occured when trying to hide the plain text note window.\n" +
                        "Although this does not happen often, it was seen a few times and is under investigation.\n" +
                        "This is a very annoying bug. Meanwhile, let me suggest a way to bypass the problem:\n" +
                        " -- Instead of closing the extended note window (by Alt-F4 or clicking 'X'),\n" +
                        " --    try hiding it by pressing the 'N' button again in the main (golden) window.\n\n" +
                        "Your feedback is very important, please contact us and report the problem if you can/want.\n\n" +
                        "Exception details:\n" +
                        ex.Message,
                        @"Framework Error: PlainTextNote_Closed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else // forceClose == true;
            {
                // Let the window close normally
                Logger.Record("\t[PlainTextNoteDialog_Close]: Closing PlainText Note window completely", "PlainTextNote", "info");
            }
            Logger.Record("[PlainTextNoteDialog_Close]: Close function finished", "PlainTextNote", "info");
        }
    }
}
