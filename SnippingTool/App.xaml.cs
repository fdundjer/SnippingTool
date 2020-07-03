using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using CommandLine;

namespace SnippingTool
{
    /// <summary>
    ///     Contains startup argument definitions.
    /// </summary>
    public class StartOptions
    {
        /// <summary>
        ///     Gets or sets the path of file that will be used to save screenshot.
        /// </summary>
        [Option('f', "file", Required = true, HelpText = "Set output file path.")]
        public string FilePath { get; set; }

        /// <summary>
        ///     Gets or sets the escape key which will be used to close the overlay.
        /// </summary>
        /// <remarks>
        ///     For possible keys <see cref="System.Windows.Input.Key" />.
        /// </remarks>
        [Option('e', "escapekey", Required = false, HelpText = "Sets the escape key to close the overlay.")]
        public Key? Key { get; set; }
    }

    public partial class App
    {
        private const string AppGuid = "Global\\CA8565BD-A5C7-45C1-A8EE-D42404429D32";
        private Mutex _mutex;

        public void ApplicationStart(object sender, StartupEventArgs e)
        {
            Parser.Default.ParseArguments<StartOptions>(e.Args)
                .WithParsed(StartOverlay).WithNotParsed(errors => { Shutdown(1); });
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, AppGuid, out var createdNew);

            if (!createdNew)
            {
                _mutex = null;

                Shutdown(1);
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }

        private void StartOverlay(StartOptions startOptions)
        {
            var filePathArgument = startOptions.FilePath;
            var closeKeyArgument = startOptions.Key;
            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(filePathArgument);
            }
            catch (Exception)
            {
                MessageBox.Show($"Specified image path '{filePathArgument}' is not valid.", "Error");
                Shutdown(1);
                return;
            }

            var directoryPath = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directoryPath))
            {
                MessageBox.Show($"Specified directory '{directoryPath}' doesn't exist.", "Error");
                Shutdown(1);
                return;
            }

            var closeKey = closeKeyArgument ?? Key.Escape;
            var thread = new Thread(() =>
            {
                new MainWindow(fullPath, closeKey).ShowDialog();
                Current.Dispatcher.Invoke(() => Current.Shutdown(1));
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }
}