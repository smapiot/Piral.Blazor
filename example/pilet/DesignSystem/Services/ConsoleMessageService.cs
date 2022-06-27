using System;

namespace DesignSystem
{
    public interface IConsoleMessageService
    {
        void LogMessageToConsole(string message);
    }

    public class ConsoleMessageService : IConsoleMessageService
    {
        public void LogMessageToConsole(string message)
        {
            Console.WriteLine($"MessageLogged: {message}");
        }
    }
}
