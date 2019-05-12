using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Application = System.Windows.Forms.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;

#pragma warning disable 612,618

namespace Rapid_Reporter.Forms
{
    // Controls the main Widget
    public partial class SmWidget
    { 
        // Session Notes variables
        private int _currentNoteType;		// The actual types are controlled by the Session class.
        private int _prevNoteType; private int _nextNoteType; // Used for the hints about the next note up or down.
        private int _currentScreenshot = 1;		// The number of the screenshot (increases by 1). Helps putting them in order, and finding them between multiple the files.
        private string _screenshotName = "";		// Attached to a Session Note.
        public string PlainTextNoteName = "";			// Attached to a Session Note. Public because it is used *directly* by the RTFNote

        //rtf and pt note things
        public bool IsPlainTextDiagOpen;

        // State Based Behaviors:
        // Session flow:
        //  1) There are two initialization stages in application: tester and charter. Then it moves to the 'testing', or 'notes' stage.

        // Session flow state based behavior
        //enum sessionStartingStage { tester, charter, notes };
        // This is used only in the beginning, in order to receive the tester name and charter text
        Session.SessionStartingStage _currentStage = Session.SessionStartingStage.Tester;

        // Timer to perform recurring actions (timing is set on windows load)
        static Timer _recurrenceTimer = new Timer();

        HotKey _hotKey;

        // These two classes are external.
        //  We share classes and data all around. This coupling will be a weak spot if app gets complex.
        Session _currentSession  = new Session();    // The session managing class
        readonly PlainTextNote _ptn = new PlainTextNote();                // The enhanced note window

        /** Starting Process **/
        /**********************/
        /// The application starts by asking for tester and charter information. Only then the session starts

        // Default constructor, everything is empty/default values
        public SmWidget()
        {
            RegUtil.InitReg();
            var trans = GetTransparencyFromReg();
            Logger.Record("[SMWidget]: App constructor. Initializing.", "SMWidget", "info");
            InitializeComponent();
            SetBgColor(GetBgColorFromReg());
            TransparencySlide.Value = trans;
            _ptn.InitializeComponent();
            _ptn.Sm = this;
            TextCompositionManager.AddPreviewTextInputHandler(NoteContent, OnPreviewTextInput);
            TextCompositionManager.AddPreviewTextInputUpdateHandler(NoteContent, OnPreviewTextInputUpdate);
            Task.Run((Action)Updater.CheckVersion);
            NoteContent.Focus();
            Logger.Record("[SMWidget]: App constructor initialized and CLI executed.", "SMWidget", "info");
        }

        // Prepare the session report log (adds the notes types, for example)
        private void SMWidgetForm_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Record("[SMWidgetForm_Loaded]: Form loading to windows", "SMWidget", "info");

            SMWidgetForm.Title = Application.ProductName;
            SetWorkingDir(_currentSession.WorkingDir);
            StateMove(Session.SessionStartingStage.Tester);

            // Some of the actions in the tool are recurrent. We do them every 30 seconds.
            _recurrenceTimer.Tick += TimerEventProcessor; // this is the function called everytime the timer expires
            _recurrenceTimer.Interval = 90 * 1000; // 30 times 1 second (1000 milliseconds)
            _recurrenceTimer.Start();

            NoteContent.Focus();
        }
        // When the widget is on focus, the note taking area is always on focus. Tester can keep writing all the time
        private void SMWidgetForm_GotFocus(object sender, RoutedEventArgs e)
        {
            Logger.Record("[SMWidgetForm_GotFocus]: SMWidget on focus", "SMWidget", "info");
            SMWidgetForm.NoteContent.Focus();
        }

        /** Closing Process **/
        /*********************/

        //// Existent notes were automatically saved in a persistent file, so no need to save now.
        //// Mainly, the session should be terminated (timing notes added to file too) and all windows closed.
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Record("[CloseButton_Click]: Closing Form...", "SMWidget", "info");
            Close();
        }
        // Closing the form can't just close the window, it has to follow the finalization process
        private void SMWidgetForm_Closed(object sender, EventArgs e)
        {
            Logger.Record("[SMWidgetForm_Closed]: Exiting Application...", "SMWidget", "info");
            ExitApp();
        }
        // Before closing the window, we have to close the session and the RTF note
        private void ExitApp(bool dontFinishSession = false)
        {
            if (!dontFinishSession)
            {
                // Session
                Logger.Record("[ExitApp]: Closing Session...", "SMWidget", "info");
                _currentSession.CloseSession();
            }
            // PT Note
            Logger.Record("[ExitApp]: Closing PlainText Note (force = true)...", "SMWidget", "info");
            _ptn.ForceClose = true; // We keep the RTF open (hidden), so we have to force it out
            Logger.Record("[ExitApp]: Closing PlainText Note...", "SMWidget", "info");
            _ptn.Close();
            // This form
            Logger.Record("[ExitApp]: End of application!", "SMWidget", "info");
            Environment.Exit(0); // Fixes mysterious crash on non-session executions (reported by AB, MB, JS)
        }

        /** Window Event Handling **/
        /***************************/

        // Application can be set transparent, to avoid distraction from task at hand
        private void TransparencySlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Logger.Record("[TransparencySlide_ValueChanged]: Changing transparency to " + e.NewValue, "SMWidget", "config");
            SetTransparency(e.NewValue);
        }

        // Application can be moved around the screen, to keep it out of the way
        void SMWidget_LeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Logger.Record("[SMWidget_LeftButtonDown]: Window dragged on screen", "SMWidget", "info");
            DragMove();
        }

        private bool isIMEinput = false;
        private int enterKeyBuffer { get; set; } //buffer for ignore Enter key at IME input

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (isIMEinput)
            {
                enterKeyBuffer = 1;
            }
            else
            {
                enterKeyBuffer = 0;
            }
            isIMEinput = false;
        }

        private void OnPreviewTextInputUpdate(object sender, TextCompositionEventArgs e)
        {
            if (e.TextComposition.CompositionText.Length == 0)
            {
                isIMEinput = false;
            }
            else
            {
                isIMEinput = true;
            }
        }

        private void NoteContent_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (isIMEinput == false && e.Key == Key.Enter && enterKeyBuffer == 1)
            {
                enterKeyBuffer = 0;
            }
            else if (isIMEinput == false && e.Key == Key.Enter && enterKeyBuffer == 0)
            {
                NoteContent_KeyUp_launch(sender, e);
            }
            else if (e.Key != Key.Enter)
            {
                NoteContent_KeyUp_launch(sender, e);
            }
        }

        /** Note Event Handling **/
        // Very important functions happen during note taking:
        //  Every submittal saves the note to disk (data is always safe)
        //  Type of note can be changed easily at all times, by pressing up/down
        private void NoteContent_KeyUp_launch(object sender, KeyEventArgs e)
        {
            var notesLenght = _currentSession.NoteTypes.Length - 1;
            
            // Up and Down cycles through the note types
            if ((e.Key == Key.Down || e.Key == Key.Up) && _currentStage == Session.SessionStartingStage.Notes)
            {
                // Algorithm is spelled out: If at end, forward goes to beginning, if at beginning back goes to end, step by 1 otherwise.
                if (e.Key == Key.Up)
                {
                    _prevNoteType = _currentNoteType;
                    if (_currentNoteType > 0)
                    { _currentNoteType--; }
                    else
                    { _currentNoteType = notesLenght; }
                    if (_nextNoteType > 0)
                    { _nextNoteType--; }
                    else
                    { _nextNoteType = notesLenght; }
                }
                else if (e.Key == Key.Down)
                {
                    _nextNoteType = _currentNoteType;
                    if (_currentNoteType < notesLenght)
                    { _currentNoteType++; }
                    else
                    { _currentNoteType = 0; }
                    if (_prevNoteType < notesLenght)
                    { _prevNoteType++; }
                    else
                    { _prevNoteType = 0; }
                }
                Logger.Record("\t[NoteContent_KeyUp]: Changing note to " + _currentSession.NoteTypes[_currentNoteType], "SMWidget", "info");
                NoteType.Text = _currentSession.NoteTypes[_currentNoteType] + ":";
                prevType.Text = "? " + _currentSession.NoteTypes[_prevNoteType] + ":";
                nextType.Text = "? " + _currentSession.NoteTypes[_nextNoteType] + ":";
            }
            else switch (e.Key)
            {
                // Enter keys accept the note into the report
                case Key.Enter:
                    if (e.Key == Key.Enter && NoteContent.Text.Trim().Length != 0)
                    {
                        Logger.Record("\t[NoteContent_KeyUp]: Enter pressed...", "SMWidget", "info");
                        switch (_currentStage)
                        {
                            case Session.SessionStartingStage.Tester:
                                _currentSession.Tester = NoteContent.Text.Replace("\"", "''").Replace(",", "").Replace(";", "").Trim();
                                StateMove(Session.SessionStartingStage.ScenarioId);
                                break;
                            case Session.SessionStartingStage.ScenarioId:
                                _currentSession.ScenarioId = NoteContent.Text.Replace("\"", "''").Replace(",", "").Replace(";", "").Trim();
                                StateMove(Session.SessionStartingStage.Charter);
                                break;
                            case Session.SessionStartingStage.Charter:
                                _currentSession.Charter = NoteContent.Text.Replace("\"", "''").Replace(",", "").Replace(";", "").Trim();
                                StateMove(Session.SessionStartingStage.Environment);
                                break;
                            case Session.SessionStartingStage.Environment:
                                _currentSession.Environment = NoteContent.Text.Replace("\"", "''").Replace(",", "").Replace(";", "").Trim();
                                StateMove(Session.SessionStartingStage.Versions);
                                break;
                            case Session.SessionStartingStage.Versions:
                                _currentSession.Versions = NoteContent.Text.Replace("\"", "''").Replace(",", "").Replace(";", "").Trim();
                                StateMove(Session.SessionStartingStage.Notes);
                                break;
                            default:
                                {
                                    // What we do when adding a note:
                                    //  - 1) We add to the session notes
                                    //  - 2) We add to the history context menu
                                    //  - 3) We clear notes and attachments to make place for new ones
                                    /*1*/
                                    _currentSession.UpdateNotes(_currentNoteType, NoteContent.Text.Replace("\"", "''").Replace(",", ";").Trim(), _screenshotName, PlainTextNoteName);
                                    /*2*/   var item = new MenuItem {Header = NoteContent.Text};
                                    item.Click += delegate { GetHistory(item.Header.ToString()); };
                                    NoteHistory.Items.Add(item);
                                    NoteHistory.Visibility = Visibility.Visible;
                                    Logger.Record("\t\t[NoteContent_KeyUp]: Note added.", "SMWidget", "info");
                                }
                                break;
                        }
                        /*3*/ ClearNote();
                    }
                    break;
                // Esc key clears the note field
                case Key.Escape:
                    Logger.Record("[NoteContent_KeyUp]: (note aborted)", "SMWidget", "info");
                    ClearNote();
                    break;
            }
        }
        
        // The function below will change the visuals of the application at the different stages (tester/charter/notes state based behavior)
        private void StateMove(Session.SessionStartingStage newStage, bool skipStartSession = false)
        {
            Logger.Record("[StateMove]: Session Stage now: " + _currentStage.ToString(), "SMWidget", "info");
            _currentStage = newStage;
            switch (_currentStage)
            {
                case Session.SessionStartingStage.Tester:
                    NoteType.Text = "Reporter:";
                    prevType.Text = ""; 
                    nextType.Text = "";
                    _prevNoteType = 1;
                    _nextNoteType = _currentSession.NoteTypes.Length - 1;
                    NoteType.FontSize = 23;
                    Logger.Record("\t[StateMove]: Session Stage moving -> Tester", "SMWidget", "info");
                    break;
                case Session.SessionStartingStage.Charter:
                    NoteType.Text = "Charter:";
                    Logger.Record("\t[StateMove]: Session Stage moving -> Charter", "SMWidget", "info");
                    break;
                case Session.SessionStartingStage.ScenarioId:
                    NoteType.Text = "Scenario ID:";
                    Logger.Record("\t[StateMove]: Session Stage moving -> ScenarioId", "SMWidget", "info");
                    break;
                case Session.SessionStartingStage.Environment:
                    NoteType.Text = "Environment:";
                    Logger.Record("\t[StateMove]: Session Stage moving -> Environment", "SMWidget", "info");
                    break;
                case Session.SessionStartingStage.Versions:
                    NoteType.Text = "Versions:";
                    Logger.Record("\t[StateMove]: Session Stage moving -> Versions", "SMWidget", "info");
                    break;
                case Session.SessionStartingStage.Notes:
                    try
                    {
                        NoteContent.ToolTip = (100 < _currentSession.Charter.Length) ? _currentSession.Charter.Remove(100) + "..." : _currentSession.Charter;
                        NoteType.Text = _currentSession.NoteTypes[_currentNoteType] + ":";
                        prevType.Text = "? " + _currentSession.NoteTypes[_prevNoteType] + ":";
                        nextType.Text = "? " + _currentSession.NoteTypes[_nextNoteType] + ":";
                        NoteType.FontSize = 21;
                        if (!skipStartSession) _currentSession.StartSession();
                        ProgressGo(90);
                        t90.IsChecked = true;
                        ScreenShot.IsEnabled = true;
                        RTFNoteBtn.IsEnabled = true;
                        ResumeSession.IsEnabled = false;
                        PauseSession.IsEnabled = true;
                        // Change the icon of the image of the buttons, to NOT appear disabled.
                        CloseButton.ToolTip = "Save and Quit";
                        SaveAndQuitOption.Header = "Save and Quit";
                        SaveAndNewOption.IsEnabled = true;
                        ScreenShotIcon.Source = new BitmapImage(new Uri("iconshot.png", UriKind.Relative));
                        RTFNoteBtnIcon.Source = new BitmapImage(new Uri("iconnotes.png", UriKind.Relative));
                        TimerMenu.IsEnabled = true;
                        Logger.Record("\t\t[StateMove]: Session Stage moving -> Notes", "SMWidget", "info");
                    }
                    catch (InvalidDirecotoryException e)
                    {
                        string msg = e.Message + " Application closed.";
                        System.Windows.MessageBox.Show(msg, "Unable to Create a Folder", MessageBoxButton.OK, MessageBoxImage.Error);
                        ExitApp(true);
                    }
                    break;
                default:
                    Logger.Record("\t[StateMove]: Session Stage moving -> NULL", "SMWidget", "error");
                    break;
            }
        }

        // AboutBox
        // Shows About dialog box with software info, contacts and credits
        private void AboutBox_Click(object sender, RoutedEventArgs e)
        {
            Logger.Record("[AboutBox_Click]: About box invoked", "SMWidget", "info");
            var about = new AboutForm();
            about.ShowDialog();
        }

        // GetHistory:
        // Note reuse (the historyNote comes from pressing the history context menu)
        void GetHistory(string historyNote)
        {
            Logger.Record("[GetHistory]: Retrieving note from history", "SMWidget", "info");
            NoteContent.Text = historyNote;
        }

        // ProgressTimer
        // Makes the progress bar progress, according to the time chosen
        private void ProgressTimer_Click(object sender, RoutedEventArgs e)
        {
            Logger.Record("[ProgressTimer_Click]: Time to end: " + sender, "SMWidget", "info");
            if (sender.ToString().Contains("1 min")) ProgressGo(1);
            if (sender.ToString().Contains("3 min")) ProgressGo(3);
            if (sender.ToString().Contains("120 min")) ProgressGo(120);
            if (sender.ToString().Contains("90 min")) { ProgressGo(90); t90.IsChecked=true; }
            if (sender.ToString().Contains("60 min")) ProgressGo(60);
            if (sender.ToString().Contains("30 min")) ProgressGo(30);
            if (sender.ToString().Contains("Stop")) ProgressGo(0);
        }
        private void ProgressGo(int time) // time is received in minutes
        {
            Logger.Record("[ProgressGo]: Time to end: " + time + " min", "SMWidget", "info");
            ProgressBackground.Value = 0;

            Logger.Record("[ProgressGo]: Hiding clock icon", "SMWidget", "info");
            timeralarm.Visibility = Visibility.Hidden;

            StopTimers();
            if (time > 0)
            {
                _currentSession.Duration = time * 60;
                var duration = new Duration(TimeSpan.FromSeconds(_currentSession.Duration));
                var timedAnimation = new DoubleAnimation(100, duration);

                // Progress Bar Repositioning
                ////
                // In order to reposition the timer in a proportional place, we do the following calculation:
                //  The position of the progress bar should be put in the percentage elapsed time from the grand total time,
                //  where the grand total time is the elapsed time until now plus the time that was chosen as the new session end.
                //              Elapsed Time
                //      ------------------------------ == Percentage of time elapsed
                //      Elapsed Time + Additional Time
                ProgressBackground.Value = 100*(
                    ((DateTime.Now - _currentSession.StartingTime).TotalSeconds) /
                    (((DateTime.Now - _currentSession.StartingTime).TotalSeconds) + _currentSession.Duration)
                    );
                Logger.Record("\t[ProgressGo]: Time calculation. Value: " + ProgressBackground.Value + "; Elapsed: " + (DateTime.Now - _currentSession.StartingTime).TotalSeconds + "; duration: " + _currentSession.Duration, "SMWidget", "info");
                
                // In order to reposition the timer at the beginning of the progress bar at every change, one should stop it before restarting;
                //  StopTimers();
                //
                // In order to keep the timer in its place and just speed up or slow down to meet the end at the current time, no
                //  other operation needs to be done.
                //ProgressBackground.Value = ProgressBackground.Value; // WTF? True = True? That's the best 'no other operation' possible?

                ProgressBackground.BeginAnimation(RangeBase.ValueProperty, timedAnimation);
            }
        }
        // Sets the progress bar to null (stopped, not seen), and hides clock icon
        private void StopTimers()
        {
            Logger.Record("[StopTimers]: Stopping timer (setting to null)", "SMWidget", "info");
            ProgressBackground.BeginAnimation(RangeBase.ValueProperty, null);
            Logger.Record("[StopTimers]: Hiding clock icon", "SMWidget", "info");
            timeralarm.Visibility = Visibility.Hidden;
        }
        // TimerEventProcessor
        // The actions in this fuction will happen every time the timer expires.
        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            Logger.Record("[TimerEventProcessor]: Will perform recurring tasks", "SMWidget", "info");
            if (100 <= ProgressBackground.Value)
            {
                Logger.Record("[TimerEventProcessor]: Time's Up! Showing timer icon", "SMWidget", "info");
                timeralarm.Visibility = Visibility.Visible;
            }
        }
        // timeralarm_MouseLeftButtonDown:
        // We'll check when the progress bar reaches the end, to trigger the time's up notification
        private void timeralarm_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Logger.Record("[timeralarm_MouseLeftButtonDown]: Time's Up timer acknowledged by user", "SMWidget", "info");
            t0.IsChecked = true;
            t120.IsChecked = false; t90.IsChecked = false; t60.IsChecked = false; t30.IsChecked = false;

            ProgressGo(0);
        }
        // timer_Checked, gets timing commands from the context menu
        //  This is used both for the checked and unchecked events. On checked, to make sure the event sets the right timer, on unchecked to make sure the timer isn't unchecked.
        private void timer_Checked(object sender, RoutedEventArgs e)
        {
            Logger.Record("[timer_Checked]: Timer context menu command: " + sender, "SMWidget", "info");
            t0.IsChecked = (t0.Equals(sender));//   || t0.IsChecked);
            t120.IsChecked = (t120.Equals(sender));// || t120.IsChecked);
            t90.IsChecked = (t90.Equals(sender));//  || t90.IsChecked);
            t60.IsChecked = (t60.Equals(sender));//  || t60.IsChecked);
            t30.IsChecked = (t30.Equals(sender));//  || t30.IsChecked);
        }

        /** Note Handling  **/
        /********************/

        // Saves all screenshots in files
        private void ScreenShot_Click(object sender, RoutedEventArgs e)
        {
            Logger.Record("[ScreenShot_Click]: Capturing screen", "SMWidget", "info");
            var edit = Control.ModifierKeys == Keys.Shift;
            var direct = Control.ModifierKeys == Keys.Control;
            if (edit || !direct) WindowState = WindowState.Minimized;
            Image imgOut;
            var ss = new ScreenShot();
            if (!direct && !edit)
            {
                imgOut = ss.CaptureSnippet();
            }
            else
            {
                imgOut = ss.CaptureScreenShot();
            }
            if (ss.Canceled)
            {
                if (edit || !direct) WindowState = WindowState.Normal;
                Logger.Record("[ScreenShot_Click]: Cancelled screenshot", "SMWidget", "info");
                return;
            }
            AddScreenshot2Note(imgOut);                                 
            Logger.Record("[ScreenShot_Click]: Captured " + _screenshotName + ", edit: " + edit, "SMWidget", "info");
            if (edit)                                                                   
            {                                                                           
                var paint = new Process
                    {
                        StartInfo =
                            {
                                FileName = "mspaint.exe",
                                Arguments = "\"" + _currentSession.WorkingDir + _screenshotName + "\""
                            }
                    };                                          
                paint.Start();                                                                          
            }
            if (edit || !direct) WindowState = WindowState.Normal; 
        }

        // Adding attached screenshot have dedicated functions that deal with the visual
        //  clues as well
        private void AddScreenshot2Note(Image bitmap)
        {
            Logger.Record("[AddScreenshot2Note]: Saving screen to file", "SMWidget", "info");
            bool exDrRetry;

            // Name the screenshot, save to disk
            _screenshotName = _currentScreenshot++.ToString(CultureInfo.InvariantCulture) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
            do
            {
                exDrRetry = false;
                try
                {
                    bitmap.Save(_currentSession.WorkingDir + _screenshotName, ImageFormat.Jpeg);
                    _currentSession.UpdateNotes("Screenshot", _screenshotName);
                }
                catch (Exception ex)
                {
                    Logger.Record("[AddScreenshot2Note]: EXCEPTION reached - Session Note file could not be saved (" + _screenshotName + ")", "SMWidget", "error");
                    exDrRetry = Logger.FileErrorMessage(ex, "SaveToSessionNotes", _screenshotName);
                }
            } while (exDrRetry);

            // Put a visual effect to remember the tester there's an image on the attachment barrel
            var effect = new BevelBitmapEffect {BevelWidth = 2, EdgeProfile = EdgeProfile.BulgedUp};
            ScreenShot.BitmapEffect = effect;
        }

        // The functions below set/unset the hotkey for screenshot
        private void SetShotHotKey_Checked(object sender, RoutedEventArgs e)
        {
            Logger.Record("[SetShotHotKey_Checked]: Will register HotKey for Screenshot now", "SMWidget", "info");
            _hotKey = new HotKey(Key.F9, KeyModifier.Ctrl | KeyModifier.Alt, OnHotKeyHandler);
        }
        private void SetShotHotKey_Unchecked(object sender, RoutedEventArgs e)
        {
            Logger.Record("[SetShotHotKey_Unchecked]: Will UNregister HotKey for Screenshot now", "SMWidget", "info");
            _hotKey.Unregister();
        }
        private void OnHotKeyHandler(HotKey hotKey)
        {
            Logger.Record("[OnHotKeyHandler]: HotKey Detected!", "SMWidget", "info");
            
            // If the user keeps the key pressed, HotKey requests will queue up and lag
            //  With this condition we break the chain, as the requests that come after stopping
            //  to press are ifnored.
            Logger.Record("[OnHotKeyHandler]: HotKey Modifiers: " + Control.ModifierKeys, "SMWidget", "info");
            if (Control.ModifierKeys != (Keys.Control | Keys.Alt))
            {
                return;
            }
            if (_currentStage == Session.SessionStartingStage.Notes)
            {
                Logger.Record("\t[OnHotKeyHandler]: Will take screenshot", "SMWidget", "info");
                ScreenShot_Click(null, null);
            }
        }

        // Show or hide the enhanced notes window.
        private void RTFNote_Click(object sender, RoutedEventArgs e)
        {

            IsPlainTextDiagOpen = !IsPlainTextDiagOpen;
            Logger.Record("[RTFNote_Click]: Will toggle PlainText note screen", "SMWidget", "info");
            if (IsPlainTextDiagOpen) // Show the note are
            {
                Logger.Record("\t[RTFNote_Click]: Repositioning the PlainText window", "SMWidget", "info");
                _ptn.Left = Left;
                _ptn.Width = Width;
                if (Top > _ptn.Height + 10)
                {
                    _ptn.Top = Top - (_ptn.Height + 10);
                }
                else
                {
                    _ptn.Top = (Top + Height) + 10;
                }
                Logger.Record(
                    "\t[RTFNote_Click]: Reposition:" + _ptn.Top + "," + _ptn.Left + "," + _ptn.Width + ",",
                    "SMWidget", "info");
                _ptn.Show();
                _ptn.Focus();
            }
            else // Hide the note area
            {
                Logger.Record("\t[RTFNote_Click]: Hiding the PlainText window", "SMWidget", "info");
                _ptn.Hide();
            }
        }

        /// Autosave Attachments
        internal void SavePlainTextNote(string filename)
        {
            Logger.Record("[SavePlainTextNote]: PlainText note saved by user (" + filename + ")", "SMWidget", "info");
            _currentSession.UpdateNotes("PlainText Note", filename);
        }

        // Makes space for a new note
        private void ClearNote()
        {
            Logger.Record("[ClearNote]: Will delete rtf note content and attachments indication", "SMWidget", "info");
            NoteContent.Text = "";  // New note
            _screenshotName = "";    // New pic attachment
            PlainTextNoteName = "";       // New note attachment (RTF note area content is left intact!)
            // Clear visual effects (screenshots are all always saved anyway)
            var effect = new BevelBitmapEffect {BevelWidth = 0, EdgeProfile = EdgeProfile.BulgedUp};
            ScreenShot.BitmapEffect = effect;
            RTFNoteBtn.BitmapEffect = effect;
        }

        // Changes the working Directory for the session
        private void SetWorkingDir(string newPath)
        {
            Logger.Record("[SetWorkingDir] Setting directory to " + newPath, "SMWidget", "info");
            if (!newPath.EndsWith(@"\")) newPath += @"\"; // Add the trailing 'slash' to the directory
            _ptn.WorkingDir = _currentSession.WorkingDir = newPath; // the workingDir needs to be the same for all files!
            //FolderName.Header = (50 < _currentSession.WorkingDir.Length) ? "..." + _currentSession.WorkingDir.Substring(_currentSession.WorkingDir.Length - 47) : _currentSession.WorkingDir;
        }

        private void SaveAndQuitOption_Click(object sender, RoutedEventArgs e)
        {
            CloseButton_Click(sender, e);
        }

        private void ResetSession()
        {
            //reset
            _currentNoteType = 0;		// The actual types are controlled by the Session class.
            _prevNoteType = 0;
            _nextNoteType = 0; // Used for the hints about the next note up or down.
            _currentScreenshot = 1;		// The number of the screenshot (increases by 1). Helps putting them in order, and finding them between multiple the files.
            _screenshotName = "";		// Attached to a Session Note.
            PlainTextNoteName = "";			// Attached to a Session Note. Public because it is used *directly* by the RTFNote
            IsPlainTextDiagOpen = false;
            ResumeSession.IsEnabled = true;
            PauseSession.IsEnabled = false;
            _currentStage = Session.SessionStartingStage.Tester;
            _recurrenceTimer = new Timer();
            _currentSession = new Session();    // The session managing class
            SMWidgetForm.Title = Application.ProductName;
            SetWorkingDir(_currentSession.WorkingDir);
            _recurrenceTimer.Tick += TimerEventProcessor; // this is the function called everytime the timer expires
            _recurrenceTimer.Interval = 90 * 1000; // 30 times 1 second (1000 milliseconds)
            _recurrenceTimer.Start();
        }

        private void SaveAndNewOption_Click(object sender, RoutedEventArgs e)
        {
            // Session
            Logger.Record("[SaveAndNewOption_Click]: Closing Session...", "SMWidget", "info");
            _currentSession.CloseSession();
            // PT Note
            Logger.Record("[SaveAndNewOption_Click]: Closing PlainText Note (force = true)...", "SMWidget", "info");
            _ptn.ForceClose = true; // We keep the RTF open (hidden), so we have to force it out
            Logger.Record("[SaveAndNewOption_Click]: Closing PlainText Note...", "SMWidget", "info");
            _ptn.Close();

            Logger.Record("[SaveAndNewOption_Click]: Resetting session variables", "SMWidget", "info");
            ResetSession();
            Logger.Record("[SaveAndNewOption_Click]: Restarting session", "SMWidget", "info");
            StateMove(Session.SessionStartingStage.Tester);
            NoteContent.Focus();
        }

        private void ColorPicker_OnClick(object sender, RoutedEventArgs e)
        {
            var colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            var r = colorDialog.Color.R;
            var g = colorDialog.Color.G;
            var b = colorDialog.Color.B;
            SetBgColor(Color.FromArgb(colorDialog.Color.A, r, g, b));
        }

        private void SetBgColor(Color color)
        {
            RegUtil.CreateRegKey("BgColor", color.ToString());
            MainGrid.Background = new SolidColorBrush(color);
        }

        private static Color GetBgColorFromReg()
        {
            var str = RegUtil.ReadRegKey("BgColor");
            if (string.IsNullOrWhiteSpace(str))
                return Color.FromArgb(byte.MaxValue, 0, 104, byte.MaxValue);
            var obj = ColorConverter.ConvertFromString(str);
            if (obj != null)
                return (Color)obj;
            return Color.FromArgb(byte.MaxValue, 0, 104, byte.MaxValue);
        }

        private void SetTransparency(Double transparency)
        {
            RegUtil.CreateRegKey("Transparency", transparency.ToString(CultureInfo.InvariantCulture));
            SMWidgetForm.Opacity = transparency;
            _ptn.Opacity = Math.Min(transparency + 0.2, 1);
        }

        private static double GetTransparencyFromReg()
        {
            var str = RegUtil.ReadRegKey("Transparency");
            if (string.IsNullOrWhiteSpace(str))
                return 1.0;
            double trans;
            return Double.TryParse(str, out trans) ? trans : 1.0;
        }

        private void ResumeSession_Click(object sender, RoutedEventArgs e)
        {
            ResetSession();
            if (!_currentSession.ResumeSession()) return;
            StateMove(Session.SessionStartingStage.Notes, true);
            _currentSession.UpdateNotes("Note", "Resuming Session...");
            NoteContent.Focus();
        }

        private void PauseSession_Click(object sender, RoutedEventArgs e)
        {
            Logger.Record("[PauseSession_Click]: Pausing Session...", "SMWidget", "info");
            _currentSession.UpdateNotes("Note", "Pausing Session...");
            ExitApp(true);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            Updater.ManualCheckVersion();
        }

        
    }
}
