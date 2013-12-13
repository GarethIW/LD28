using System;

namespace LD28
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (LD28Game game = new LD28Game())
            {
                game.Run();
            }
        }
    }
#endif
}

