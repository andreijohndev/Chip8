using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Chip8
{
    public class Chip8
    {
        public const int ScreenWidth = 64;
        public const int ScreenHeight = 32;
        public static readonly Dictionary<byte, KeyboardKey> KeyMap = new() 
        {
            { 0x1, KeyboardKey.KEY_ONE },
            { 0x2, KeyboardKey.KEY_TWO },
            { 0x3, KeyboardKey.KEY_THREE },
            { 0xC, KeyboardKey.KEY_FOUR },
            { 0x4, KeyboardKey.KEY_Q },
            { 0x5, KeyboardKey.KEY_W },
            { 0x6, KeyboardKey.KEY_E },
            { 0xD, KeyboardKey.KEY_R },
            { 0x7, KeyboardKey.KEY_A },
            { 0x8, KeyboardKey.KEY_S },
            { 0x9, KeyboardKey.KEY_D },
            { 0xE, KeyboardKey.KEY_F },
            { 0xA, KeyboardKey.KEY_Z },
            { 0x0, KeyboardKey.KEY_X },
            { 0xB, KeyboardKey.KEY_C },
            { 0xF, KeyboardKey.KEY_V }
        };

        private byte[] _ram = new byte[4096]; 
        private ushort[] _stack = new ushort[16];

        private byte[] _v = new byte[16];
        private ushort _pc = 0x200;
        private ushort _i;
        private ushort _sp;

        private byte _delayTimer;
        private byte _soundTimer;

        private ushort _currentOpcode;

        private byte[,] _screen = new byte[ScreenWidth, ScreenHeight];
        private Random _random = new Random();

        private bool _waiting;
        private byte _waitRegister;
        private bool _pendingKeyUp;
        private byte _pendingKey;

        private byte[] _font = new byte[]
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };

        public Chip8(byte[] program)
        {
            _font.CopyTo(_ram, 0);
            program.CopyTo(_ram, 0x200);
            _pc = 0x200;
        }

        public void Tick()
        {
            if (_waiting)
            {
                if (_pendingKeyUp)
                {
                    if (Raylib.IsKeyUp(KeyMap[_pendingKey]))
                    {
                        _waiting = false;
                        _v[_waitRegister] = _pendingKey;
                        _pc += 2;
                        _pendingKeyUp = false;
                        _pendingKey = 0;
                    }
                }
                else
                {
                    foreach (var key in KeyMap)
                    {
                        if (Raylib.IsKeyDown(key.Value))
                        {
                            _pendingKeyUp = true;
                            _pendingKey = key.Key;
                        }
                    }
                }

                return;
            }

            ushort opcode = (ushort)(_ram[_pc] << 8 | _ram[_pc + 1]);
            _currentOpcode = opcode;

            switch(opcode & 0xF000)
            {
                case 0x0000:
                    switch (opcode & 0x00FF)
                    {
                        case 0x00E0:
                            ClearScreen();
                            break;
                        case 0x00EE:
                            ReturnFromSubroutine();
                            break;
                        default:
                            Console.WriteLine($"Unrecognized opcode {opcode}");
                            throw new Exception();
                    }
                    break;
                case 0x1000:
                    Jump();
                    break;
                case 0x2000:
                    Call();
                    break;
                case 0x3000:
                    SkipIfVxEqual();
                    break;
                case 0x4000:
                    SkipIfVxNotEqual();
                    break;
                case 0x5000:
                    SkipIfVxAndVyEqual();
                    break;
                case 0x6000:
                    SetVx();
                    break;
                case 0x7000:
                    AddVx();
                    break;
                case 0x8000:
                    switch (opcode & 0x000F)
                    {
                        case 0x0000:
                            SetVxToVy();
                            break;
                        case 0x0001:
                            ORVxVy();
                            break;
                        case 0x0002:
                            ANDVxVy();
                            break;
                        case 0x0003:
                            XORVxVy();
                            break;
                        case 0x0004:
                            AddVxVy();
                            break;
                        case 0x0005:
                            SubtractVxVy();
                            break;
                        case 0x0006:
                            ShiftVxRight();
                            break;
                        case 0x0007:
                            SubtractVyVx();
                            break;
                        case 0x000E:
                            ShiftVxLeft();
                            break;
                        default:
                            Console.WriteLine($"Unrecognized opcode {opcode}");
                            break;
                    }
                    break;
                case 0x9000:
                    SkipIfVxNotEqualVy();
                    break;
                case 0xA000:
                    SetI();
                    break;
                case 0xB000:
                    JumpV0();
                    break;
                case 0xC000:
                    SetVxToRandom();
                    break;
                case 0xD000:
                    DrawSprite();
                    break;
                case 0xE000:
                    switch (opcode & 0x00FF)
                    {
                        case 0x009E:
                            SkipIfKeyPressed();
                            break;
                        case 0x00A1:
                            SkipIfKeyNotPressed();
                            break;
                        default:
                            Console.WriteLine($"Unrecognized opcode {opcode}");
                            break;
                    }
                    break;
                case 0xF000:
                    switch (opcode & 0x00FF)
                    {
                        case 0x0007:
                            SetVxToDelayTimer();
                            break;
                        case 0x000A:
                            WaitForInput();
                            break;
                        case 0x0015:
                            SetDelayTimer();
                            break;
                        case 0x0018:
                            SetSoundTimer();
                            break;
                        case 0x001E:
                            AddI();
                            break;
                        case 0x0029:
                            SetIToSpriteLocation();
                            break;
                        case 0x0033:
                            StoreBcd();
                            break;
                        case 0x0055:
                            StoreVInMemory();
                            break;
                        case 0x0065:
                            LoadVFromMemory();
                            break;
                    }
                    break;
                default:
                    Console.WriteLine($"Unrecognized opcode {opcode}");
                    break;
            }
        }

        public void UpdateTimers()
        {
            if (_delayTimer > 0)
            {
                _delayTimer--;
            }

            if (_soundTimer > 0)
            {
                _soundTimer--;
            }
        }

        public void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);
            for (int x = 0; x < ScreenWidth; x++)
            {
                for (int y = 0; y < ScreenHeight; y++)
                {
                    if (_screen[x, y] != 0)
                    {
                        Raylib.DrawRectangle(x * 20, y * 20, 20, 20, Color.RAYWHITE);
                    }
                }
            }
            Raylib.EndDrawing();
        }

        private void ClearScreen()
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                for (int y = 0; y < ScreenHeight; y++)
                {
                    _screen[x, y] = 0;
                }
            }
            _pc += 2;
        }

        private void ReturnFromSubroutine()
        {
            _sp--;
            _pc = _stack[_sp];
            _pc += 2;
        }

        private void Jump()
        {
            ushort nnn = (ushort)(_currentOpcode & 0x0FFF);
            _pc = nnn;
        }

        private void Call()
        {
            ushort nnn = (ushort)(_currentOpcode & 0x0FFF);
            _stack[_sp] = _pc;
            _sp++;
            _pc = nnn;
        }

        private void SkipIfVxEqual()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte kk = (byte)(_currentOpcode & 0x00FF);

            _pc += (ushort)(_v[x] == kk ? 4 : 2);
        }

        private void SkipIfVxNotEqual()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte kk = (byte)(_currentOpcode & 0x00FF);

            _pc += (ushort)(_v[x] != kk ? 4 : 2);
        }

        private void SkipIfVxAndVyEqual()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);

            _pc += (ushort)(_v[x] == _v[y] ? 4 : 2);
        }

        private void SetVx()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte kk = (byte)(_currentOpcode & 0x00FF);

            _v[x] = kk;
            _pc += 2;
        }

        private void AddVx()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte kk = (byte)(_currentOpcode & 0x00FF);

            _v[x] += kk;
            _pc += 2;
        }

        private void SetVxToVy()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);

            _v[x] = _v[y];
            _pc += 2;
        }

        private void ORVxVy()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);

            _v[x] |= _v[y];
            _pc += 2;
        }

        private void ANDVxVy()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);

            _v[x] &= _v[y];
            _pc += 2;
        }

        private void XORVxVy()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);

            _v[x] ^= _v[y];
            _pc += 2;
        }

        private void AddVxVy()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);
            ushort sum = (ushort)(_v[x] + _v[y]);

            _v[x] += _v[y];
            _v[0xF] = (byte)(sum > 255 ? 1 : 0);
            _pc += 2;
        }

        private void SubtractVxVy()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);
            byte vXRegister = _v[x];
            byte vYRegister = _v[y];

            _v[x] -= _v[y];
            _v[0xF] = (byte)(vXRegister > vYRegister ? 1 : 0);
            _pc += 2;
        }

        private void ShiftVxRight()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte vXRegister = _v[x];

            _v[x] >>= 1;
            _v[0xF] = (byte)(vXRegister & 0x1);
            _pc += 2;
        }

        private void SubtractVyVx()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);
            byte vXRegister = _v[x];
            byte vYRegister = _v[y];

            _v[x] = (byte)(_v[y] - _v[x]);
            _v[0xF] = (byte)(vYRegister > vXRegister ? 1 : 0);
            _pc += 2;
        }

        private void ShiftVxLeft()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte vXRegister = _v[x];

            _v[x] <<= 1;
            _v[0xF] = (byte)((vXRegister & 0x80) >> 7);
            _pc += 2;
        }

        private void SkipIfVxNotEqualVy()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);

            _pc += (ushort)(_v[x] != _v[y] ? 4 : 2);
        }

        private void SetI()
        {
            ushort nnn = (ushort)(_currentOpcode & 0x0FFF);

            _i = nnn;
            _pc += 2;
        }

        private void JumpV0()
        {
            ushort nnn = (ushort)(_currentOpcode & 0x0FFF);

            _pc = (ushort)(nnn + _v[0]); // TODO: Configurable to make it jump to VX instead of V0
        }

        private void SetVxToRandom()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte kk = (byte)(_currentOpcode & 0x00FF);

            _v[x] = (byte)(_random.Next(0, 256) & kk);
            _pc += 2;
        }

        private void DrawSprite()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            byte y = (byte)((_currentOpcode & 0x00F0) >> 4);
            byte nibble = (byte)(_currentOpcode & 0x000F);
            byte startX = _v[x];
            byte startY = _v[y];

            _v[0xF] = 0;
            for (int spriteY = 0; spriteY < nibble; spriteY++)
            {
                byte line = _ram[_i + spriteY];
                
                for (int spriteX = 0; spriteX < 8; spriteX++)
                {
                    int pixelX = (_v[x] + spriteX) % ScreenWidth;
                    int pixelY = (_v[y] + spriteY) % ScreenHeight;
                    byte pixel = (byte)((line >> (7 - spriteX)) & 0x1);
                    byte oldPixel = _screen[pixelX, pixelY];

                    if ((x % ScreenWidth) + spriteX >= ScreenWidth || (y % ScreenHeight) + spriteY >= ScreenHeight)
                    {
                        continue;
                    }

                    _screen[pixelX, pixelY] ^= pixel;

                    if (oldPixel != 0 && _screen[pixelX, pixelY] == 0)
                    {
                        _v[0xF] = 1;
                    }
                }
            }

            _pc += 2;
        }

        private void SkipIfKeyPressed()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);
            
            _pc += (byte)(Raylib.IsKeyDown(KeyMap[_v[x]]) ? 4 : 2);
        }

        private void SkipIfKeyNotPressed()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            _pc += (byte)(Raylib.IsKeyUp(KeyMap[_v[x]]) ? 4 : 2);
        }

        private void SetVxToDelayTimer()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            _v[x] = _delayTimer;
            _pc += 2;
        }

        private void WaitForInput()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            _waiting = true;
            _waitRegister = x;
        }

        private void SetDelayTimer()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            _delayTimer = _v[x];
            _pc += 2;
        }

        private void SetSoundTimer()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            _soundTimer = _v[x];
            _pc += 2;
        }

        private void AddI()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            _i = (ushort)(_i + _v[x]);
            _pc += 2;
        }

        private void SetIToSpriteLocation()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            _i = (ushort)(_v[x] * 5);
            _pc += 2;
        }

        private void StoreBcd()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            _ram[_i] = (byte)(_v[x] / 100);
            _ram[_i + 1] = (byte)(_v[x] / 10 % 10);
            _ram[_i + 2] = (byte)(_v[x] % 100 % 10);
            _pc += 2;
        }

        private void StoreVInMemory()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            for (int i = 0; i <= x; i++)
            {
                _ram[_i + i] = _v[i];
            }

            _pc += 2;
        }

        private void LoadVFromMemory()
        {
            byte x = (byte)((_currentOpcode & 0x0F00) >> 8);

            for (int i = 0; i <= x; i++)
            {
                _v[i] = _ram[_i + i];
            }

            _pc += 2;
        }
    }
}
