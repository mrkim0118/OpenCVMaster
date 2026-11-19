using System;
using System.Linq;
using System.Windows.Forms;
using OpenCVMaster.Core;
using OpenCVMaster.UI;

namespace OpenCVMaster;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        // Runs every operation headless and writes selftest.log (used for verification)
        if (args.Contains("--selftest")) return SelfTest.Run();

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(args.FirstOrDefault()));
        return 0;
    }
}

