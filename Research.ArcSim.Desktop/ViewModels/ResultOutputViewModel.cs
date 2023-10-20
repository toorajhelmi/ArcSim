using Prism.Mvvm;
using Research.ArcSim.Extensions;

namespace Research.ArcSim.Desktop.ViewModels
{
    public class ResultOutputViewModel : BindableBase, IConsole
    {
        public string Output
        {
            get;
            set;
        }

        public ResultOutputViewModel()
		{
        }

        public void Clear()
        {
            Output = "";
            RaisePropertyChanged(nameof(Output));
        }

        public void Write(string text, params int[] parameters)
        {
            Output += text;
            RaisePropertyChanged(nameof(Output));
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

