using System.Windows;
using SubModelSelectedItemRecursive.DynamicBindings.MRE.Core;

namespace SubModelSelectedItemRecursive.DynamicBindings.MRE.WPF
{
    public partial class App : Application
    {
        public App()
        {
            this.Activated += StartElmish;
        }

        private void StartElmish(object sender, EventArgs e)
        {
            this.Activated -= StartElmish;
            Program.main(MainWindow);
        }

    }
}