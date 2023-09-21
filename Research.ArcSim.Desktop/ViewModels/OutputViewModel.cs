using System;
using Prism.Mvvm;
using Research.ArcSim.Extensions;

namespace Research.ArcSim.Desktop.ViewModels
{
	public class OutputViewModel : BindableBase, IConsole
    {
        public string Output
        {
            get;
            set;
        }

        public OutputViewModel()
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

