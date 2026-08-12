using System;
using Asterra.Gameplay;

namespace Asterra.Smoke
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            int ticks = 2000;
            if (args.Length > 0 && int.TryParse(args[0], out int parsed))
                ticks = parsed;

            Console.WriteLine(SkirmishSmokeTest.Run(ticks));
            return 0;
        }
    }
}
