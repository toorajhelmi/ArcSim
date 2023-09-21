using System;
namespace Research.ArcSim.Extensions
{
	public interface IConsole
	{
        void WriteLine();
        void WriteLine(string text);
        void Write(string text, params int[] parameters);
        void ShowProgress(string text, params int[] parameters);
    }
}

