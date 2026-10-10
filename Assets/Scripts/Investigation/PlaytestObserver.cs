using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Investigation
{
    // Hidden playtest logging for the developer. It runs only when a file named "playtest.on" sits in the game folder
    // (next to the .exe, or the project folder in the editor). The player sees nothing on screen.
    // Output: PlaytestLogs/<date-time>/events.csv and report.md. No names or personal information are written.
    public static class PlaytestObserver
    {
        public const string FlagFileName = "playtest.on";
        public const string LogFolderName = "PlaytestLogs";
        public const string EventsFileName = "events.csv";
        public const string ReportFileName = "report.md";

        static PlaytestRecorder recorder;
        static StreamWriter writer;
        static DateTime startedLocal;

        public static bool Active => recorder != null;
        public static string SessionFolder { get; private set; }
        public static PlaytestRecorder Recorder => recorder;
        public static string GameFolder => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static bool FlagPresent
        {
            get { try { return File.Exists(Path.Combine(GameFolder, FlagFileName)); } catch { return false; } }
        }

        // Called once when the investigation scene starts. Does nothing unless the flag file exists.
        public static void BeginIfRequested(Func<string> nameProvider)
        {
            End();
            if (!FlagPresent) return;
            if (!TryBegin(Path.Combine(GameFolder, LogFolderName), nameProvider))
                TryBegin(Path.Combine(Application.persistentDataPath, LogFolderName), nameProvider);
        }

        public static bool TryBegin(string logRoot, Func<string> nameProvider)
        {
            End();
            try
            {
                startedLocal = DateTime.Now;
                string baseFolder = Path.Combine(logRoot, startedLocal.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
                string folder = baseFolder;
                for (int i = 2; Directory.Exists(folder); i++) folder = baseFolder + "-" + i;
                Directory.CreateDirectory(folder);
                writer = new StreamWriter(Path.Combine(folder, EventsFileName), false, new UTF8Encoding(true)) { AutoFlush = true };
                writer.WriteLine(PlaytestRecorder.CsvHeader);
                SessionFolder = folder;
                recorder = new PlaytestRecorder(() => Time.realtimeSinceStartupAsDouble, WriteLine, nameProvider);
                PlaytestObserverRunner.Ensure();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Playtest log unavailable: " + e.Message);
                recorder = null; SessionFolder = null;
                try { writer?.Dispose(); } catch { }
                writer = null;
                return false;
            }
        }

        static void WriteLine(string line)
        {
            try { writer?.WriteLine(line); }
            catch (Exception e) { Debug.LogWarning("Playtest log write failed: " + e.Message); }
        }

        // Writes report.md now. Safe to call several times; the latest call wins.
        public static string WriteReport()
        {
            if (recorder == null || SessionFolder == null) return null;
            try
            {
                string path = Path.Combine(SessionFolder, ReportFileName);
                File.WriteAllText(path, recorder.BuildReport(Application.version, startedLocal), new UTF8Encoding(true));
                return path;
            }
            catch (Exception e) { Debug.LogWarning("Playtest report failed: " + e.Message); return null; }
        }

        public static void End()
        {
            if (recorder == null) return;
            Safe(r => r.Finish());
            WriteReport();
            try { writer?.Dispose(); } catch { }
            writer = null; recorder = null;
        }

        static void Safe(Action<PlaytestRecorder> action)
        {
            if (recorder == null) return;
            try { action(recorder); }
            catch (Exception e) { Debug.LogWarning("Playtest log skipped an event: " + e.Message); }
        }

        internal static void Tick(bool anyInput, bool focused) => Safe(r => r.Tick(anyInput, focused));
        public static void Screen(string category, string heading, int bodyLength) => Safe(r => r.Screen(category, heading, bodyLength));
        public static void ModalClosed() => Safe(r => r.ModalClosed());
        public static void Click(string label, bool interactable) => Safe(r => r.Click(label, interactable));
        public static void Location(string name) => Safe(r => r.Location(name));
        public static void Objective(string text) => Safe(r => r.Objective(text));
        public static void Journal() => Safe(r => r.Journal());
        public static void Pause() => Safe(r => r.Pause());
        public static void SourceRead(string title) => Safe(r => r.SourceRead(title));
        public static void Judgment(int question, string option, bool ready, bool correct) => Safe(r => r.Judgment(question, option, ready, correct));
        public static void ReportOpened() => Safe(r => r.ReportOpened());
        public static void ReportCancelled() => Safe(r => r.ReportCancelled());
        public static void ReportEdited(string what) => Safe(r => r.ReportEdited(what));
        public static void ReportSubmitted(string conclusion, IEnumerable<string> proofs, bool decoy, string action, string ending)
        {
            Safe(r => r.ReportSubmitted(conclusion, proofs, decoy, action, ending));
            WriteReport();
        }
    }
}
