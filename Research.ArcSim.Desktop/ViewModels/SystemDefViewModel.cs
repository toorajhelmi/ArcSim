using Prism.Mvvm;
using Research.ArcSim.Extensions;

namespace Research.ArcSim.Desktop.ViewModels
{
    public class SystemDefViewModel : BindableBase, IConsole
    {
        public string Output { get; set; }

        public SystemDefViewModel()
        {
        }

        public void Write(string text, params int[] parameters)
        {
            Output += text;
        }

        public void WriteLine()
        {
            Output += "\n";
            RaisePropertyChanged(nameof(Output));
        }

        public void WriteLine(string text)
        {
            Output += $"{text}\n";
            RaisePropertyChanged(nameof(Output));
        }


        public void ShowProgress(string text, params int[] parameters)
        {
        }
    }
}

