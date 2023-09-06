using Timer = System.Timers.Timer;
using static Raylib_cs.Raylib;
using System.Diagnostics;

namespace Chip8
{
    public class Program
    {
        public const int WindowWidth = 1280;
        public const int WindowHeight = 640;
        public const int TickRate = 20;

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: ./chip8 path/to/rom");
                return 1;
            }

            try
            {
                if (File.Exists(args[0]))
                {
                    byte[] rom = File.ReadAllBytes(args[0]);
                    Chip8 chip8 = new Chip8(rom);

                    Stopwatch sw = Stopwatch.StartNew();
                    TimeSpan lastTime = TimeSpan.Zero;
                    TimeSpan interval = TimeSpan.FromMilliseconds(1000.0 / 60.0); // 60 fps (16 ms)

                    InitWindow(WindowWidth, WindowHeight, "CHIP-8");
                    SetTargetFPS(60);

                    while (!WindowShouldClose())
                    {
                        var elapsed = sw.Elapsed - lastTime;

                        if (elapsed >= interval)
                        {
                            for (int i = 0; i < TickRate; i++)
                            {
                                chip8.Tick();
                            }
                            chip8.UpdateTimers();
                        }

                        lastTime = sw.Elapsed;

                        chip8.Draw();
                    }

                    CloseWindow();
                }
                else
                {
                    Console.WriteLine("File not found");
                    return 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e}");
                return 1;
            }

            return 0;
        }
    }
}