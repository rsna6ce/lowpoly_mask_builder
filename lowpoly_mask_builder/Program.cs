using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lowpoly_mask_builder
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string fileToOpen = null;
            // コマンドライン引数
            if (args.Length > 0)
            {
                string arg = args[0];
                if (arg.EndsWith(".lmb", StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(arg))
                {
                    fileToOpen = arg;
                }
            }

            // Form1 にファイルパスを渡して起動
            Application.Run(new Form1(fileToOpen));

        }
    }
}
