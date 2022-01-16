using System.Diagnostics;
using System.Threading;

namespace Mario
{
    public static class Util
    {
        public static void trade_alarm(string buy_or_sell)
        {
            var sound_file = "";
            switch (buy_or_sell)
            {
                case "Buy":
                    sound_file = "trade-bought.mp3";
                    break;

                case "Sell":
                    sound_file = "trade-sold.mp3";
                    break;
            }
            var t = new Thread(() =>
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = Global.file_sound_player,
                    Arguments = Global.directory_sound + sound_file,
                };
                Process proc = new Process()
                {
                    StartInfo = startInfo,
                };
                proc.Start();
                proc.WaitForExit();
            });
            t.Start();
        }
    }
}