using System;
using System.Collections.Generic;
//using System.Linq;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace EON_FIRMWARE_FLASH
{
   
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
      
        [STAThread]
         //= new List<string>();
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
