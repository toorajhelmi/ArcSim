using Research.ArcSim.Extensions;

namespace Research.ArcSim.Terminal
{
    public class SystemConsole : IConsole
    {
        public void Write(string text, params int[] parameters)
        {
            Console.Write(text, parameters);
        }

        public void ShowProgress(string text, params int[] parameters)
        {
            Write(text, parameters);
        }

        public void WriteLine()
        {
            Console.WriteLine();
        }

        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }
    }
}

