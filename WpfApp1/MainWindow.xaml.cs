using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string adbPath = @"D:\ADB\adb.exe";
        Process process = new Process();
        public MainWindow()
        {
            InitializeComponent();
            /*string adbCommand = "devices";*/

            process.StartInfo.FileName = adbPath;
            process.StartInfo.Arguments = "devices";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            // Запуск процесса и получение вывода команды
            process.Start();
            process.WaitForExit();

        }
        private async Task<bool> IsEmptyInRecentItemsListWindow()
        {
            ///// Використовуємо дату для перевірки на факт того, що ми знаходимось на домашній сторінці телефону /////
            //// shell dumpsys window windows | grep -E 'mCurrentFocus|mFocusedApp'\r\n не відрізняє домашню сторінку від сторінки з recent-app /////// 
     
            DateTime currentDate = DateTime.Now;
            CultureInfo englishCulture = new CultureInfo("en-US");
            string formattedDate = currentDate.ToString("dddd, MMM d", englishCulture);
            string output = string.Empty;

            while (!output.Contains("xml"))
            {
                string adbGetTextCommand = "shell uiautomator dump /dev/tty ";
                process.StartInfo.Arguments = adbGetTextCommand;

                process.Start();
                Task<string> taskOutput = process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                output = taskOutput.Result;
            }
            //// Отримали хмл і перевіряємо на те, чи закрили ми всі аплікухи ///
            return output.Contains(formattedDate) || output.Contains("No recent items");
        }
        private int[] GetViewCoordinates(in string boundsString)
        {
            var replacedBoundString = boundsString
                    .Replace("bounds=", "")
                    .Replace("[", "")
                    .Replace("]", ",")
                    .Replace("\"", "")
                    .Split(',');

            int startX = int.Parse(replacedBoundString[0]);
            int startY = int.Parse(replacedBoundString[1]);
            int endX = int.Parse(replacedBoundString[2]);
            int endY = int.Parse(replacedBoundString[3]);

            int X = (startX + endX) / 2;
            int Y = (startY + endY) / 2;
            return new int[] { X, Y };
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            string adbHomeButtonCommand = "shell input keyevent KEYCODE_HOME";
            process.StartInfo.Arguments = adbHomeButtonCommand;
            process.Start();
            await process.WaitForExitAsync();

            string adbSwitchButtonCommand = "shell input keyevent KEYCODE_APP_SWITCH";
            process.StartInfo.Arguments = adbSwitchButtonCommand;
            process.Start();
            await process.WaitForExitAsync();

            //// Поки не попадемо на домашнью або no recent items сторінку - виконуємо цикл ///
            while (!await IsEmptyInRecentItemsListWindow())
            {
                string adbScrollCommand = "shell input swipe 500 1000 500 300";
                process.StartInfo.Arguments = adbScrollCommand;
                process.Start();
                await process.WaitForExitAsync();
            }

            process.StartInfo.Arguments = adbHomeButtonCommand;
            process.Start();
            await process.WaitForExitAsync();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string currentProcess = string.Empty;
            while (!currentProcess.Contains("mCurrentFocus"))
            {
                string adbInitializeBrowserCommand = "shell dumpsys window windows | grep -E 'mCurrentFocus|mFocusedApp'\r\n";
                process.StartInfo.Arguments = adbInitializeBrowserCommand;
                process.Start();
                currentProcess = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
            }

            if (!currentProcess.Contains("chrome.Main")) 
            {
                string adbHomeButtonCommand = "shell input keyevent KEYCODE_HOME";
                process.StartInfo.Arguments = adbHomeButtonCommand;
                process.Start();
                await process.WaitForExitAsync();

                string mainPageXmlContent = string.Empty;
                while (!mainPageXmlContent.Contains("xml"))
                {
                    string adbGetMainHomeCommand = "shell uiautomator dump /dev/tty";
                    process.StartInfo.Arguments = adbGetMainHomeCommand;
                    process.Start();
                    mainPageXmlContent = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                }

                var boundsMainHomeString =
                mainPageXmlContent.Split("text=\"Chrome\"")
                .Last()
                .Split(" ")
                .FirstOrDefault(_ => _.StartsWith("bounds=")); //можна звісно писати парсер XML але в даному випадку це легше та профітней

                int[] chromeAppCoordinates = GetViewCoordinates(boundsMainHomeString);
                var X_ChromeApp = chromeAppCoordinates[0];
                var Y_ChromeApp = chromeAppCoordinates[1];

                string adbChromeAppTapCommand = $"shell input tap {X_ChromeApp} {Y_ChromeApp}";
                process.StartInfo.Arguments = adbChromeAppTapCommand;
                process.Start();
                await process.WaitForExitAsync();
            }

            ////////////// Варіант для більш легкого запуску браузера //////////////
            /*string adbStartBrowserCommand = "shell am start -n com.android.chrome/com.google.android.apps.chrome.Main";
            process.StartInfo.Arguments = adbStartBrowserCommand;
            process.Start();
            await process.WaitForExitAsync();*/

            string xmlContent = string.Empty;
            // через деяку затримку та багованість мого емулятора, потрібно перевіряти чи встиг у нас виконатися тап та чи перейшли ми до браузера
            while (!xmlContent.Contains("xml")) 
            {
                string adbGetPageXMLCommand = "shell uiautomator dump /dev/tty";
                process.StartInfo.Arguments = adbGetPageXMLCommand;
                process.Start();
                xmlContent = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
            }
            var boundsString =
                xmlContent.Split("class=\"android.widget.EditText\"")
                .Last()
                .Split(" ")
                .FirstOrDefault(_ => _.StartsWith("bounds=")); //можна звісно писати парсер XML але в даному випадку це легше та профітней

            int[] editTextCoordinates = GetViewCoordinates(boundsString);
            var X_EditText = editTextCoordinates[0];
            var Y_EditText = editTextCoordinates[1];

            string adbEditTextTapCommand = $"shell input touchscreen swipe {X_EditText} {Y_EditText} {X_EditText} {Y_EditText} 1000";
            process.StartInfo.Arguments = adbEditTextTapCommand;
            process.Start();
            await process.WaitForExitAsync();


            string adbDeletePreviousTextCommand = "shell input keyevent KEYCODE_DEL";
            process.StartInfo.Arguments = adbDeletePreviousTextCommand;
            process.Start();
            await process.WaitForExitAsync();

            string adbInputTextCommand = $"shell input text \"{inputTextBox.Text}\"";
            process.StartInfo.Arguments = adbInputTextCommand;
            process.Start();
            await process.WaitForExitAsync();

            string adbSearchCommand = "shell input keyevent 66";
            process.StartInfo.Arguments = adbSearchCommand;
            process.Start();
            await process.WaitForExitAsync();
        }
    }
}
