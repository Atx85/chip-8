// https://en.wikipedia.org/wiki/CHIP-8
using System;
using System.IO;
using System.Collections.Generic;
namespace Chip8 {
interface IBus { 
  void Write(short addr, byte val); 
  byte Read(short addr); 
  void StackPush(byte val);
  byte StackPop();
  byte Key();
  void PressKey(byte keys);
}
interface ICpu { int Step(); }
interface IInstructionDecoder { void Decode(short opcode, ref short pc); }

class Bus : IBus {
  private byte[] memory; 
  private Stack<byte> stack;
  private byte key; // keyboard hex 0 .. 15

  public Bus(byte[] rom) {
    memory = new byte[0x1000];
    stack = new Stack<byte>();
    Fonts fonts = new Fonts();
    for (int i = 0; i < fonts.chip8Fontset.Length; i++) {
      memory[i] = fonts.chip8Fontset[i];
    }
    for (int i = 0; i < rom.Length; i++) {
      memory[0x200 + i] = rom[i];
    }
  }
  public void StackPush(byte val) {
    stack.Push(val);
  }
  public byte StackPop() => stack.Pop();
  public byte Read(short addr) =>  memory[addr];
  public void Write(short addr, byte val) => memory[addr] = val;
  public byte Key() => key;
  public void PressKey(byte keys) => key = keys;
}

public struct Instruction {
  public short op; 
  public string type;
  public string c;
  public short operand;
  public byte x, y;
}

class Registers {
  public byte[] V;
  public short I;
  public short pc;

  public Registers () {
    V = new byte[16];
    pc = 0x200;
  }
}
class Timer {
  private byte time;
  public byte Time { get; set;}
  public Timer () { Time = 0; }
  public void Advance () {
    if (Time > 0) Time --;
  }
}
class Cpu : ICpu {
  private IBus bus;
  private InstructionDecoder decoder;
  private Registers regs;
  private Timer delayTimer, soundTimer;
  private bool[] frameBuffer;

  public Cpu(IBus paramBus) {
    bus = paramBus;
    decoder = new InstructionDecoder();
    regs = new Registers();
    delayTimer = new Timer();
    soundTimer = new Timer();
    frameBuffer = new bool[64 * 32];
  }
  void Call(Instruction inst) {
    byte l = (byte)(regs.pc & 0x00FF);
    byte h = (byte)((regs.pc & 0xFF00) >> 8);
    bus.StackPush(l);
    bus.StackPush(h);
    regs.pc = (short)(inst.operand);
  }
  void Ret() {
    byte h = bus.StackPop();
    byte l = bus.StackPop();
    short newAddr = (short)((h << 8) | l);
    regs.pc = newAddr; 
  }
  byte coord (byte x, byte y) => (byte)(y * 32 + x);
  void Draw(byte x, byte y, short N) {
    // draws a sprite at x,y 8 wide, N high starting from memory location I, 
    // VF is set to 1 if any screen pixels are flipped from set to unset and 0 if that doesn't happen
     int row = y;
     for (int actualRow = row; actualRow < 8; actualRow++) {
       int x0 = (int) coord(x, (byte)actualRow); 
       int xn = x0 + 8;// this might need more 
       Console.WriteLine("Drawing: ");
       for (int w = x0; w < xn; w++) {
         Console.Write($"{bus.Read(regs.I):X4} ");
         // do something here and around for N
       } 
     }
     Console.WriteLine($"\nI: {regs.I:X4} X: {x} Y: {y} N: {N}");
  }
  void DisplayClear() {
    frameBuffer = new bool[64 * 32];
  }
  void Execute(Instruction inst, ref Registers regs) {
    switch (inst.type) {
      case "0NNN": {Call(inst); break; }
      case "00E0": {DisplayClear();break;}
      case "00EE": {Ret();break;}
      case "1NNN": {regs.pc = inst.operand;break;}
      case "2NNN": {
                    Call(inst);
                    break;
                   }
      case "3XNN": {
                     if (regs.V[inst.x] == inst.operand) regs.pc+=2;
                     break;
                   }
      case "4XNN": {
                     if (regs.V[inst.x] != inst.operand) regs.pc+=2;
                     break;
                   }
      case "5XY0": {
                     if (regs.V[inst.x] == regs.V[inst.y]) regs.pc+=2;
                     break;
                   }
      case "6XNN": {
                     regs.V[inst.x] = (byte)inst.operand;
                     break;
                   }
      case "7XNN": {  
                     regs.V[inst.x] += (byte)inst.operand;
                     break;
                   }
      case "8XY0": {  
                     regs.V[inst.x]  = regs.V[inst.x];
                     break;
                   }
      case "8XY1": {  
                     regs.V[inst.x]  |= regs.V[inst.x];
                     break;
                   }
      case "8XY2": {  
                     regs.V[inst.x]  &= regs.V[inst.x];
                     break;
                   }
      case "8XY3": {  
                     regs.V[inst.x]  ^= regs.V[inst.x];
                     break;
                   }
      case "8XY4": {  
                     regs.V[inst.x]  += regs.V[inst.x];
                     break;
                   }
      case "8XY5": {  
                     regs.V[inst.x]  -= regs.V[inst.x];
                     break;
                   }
      case "8XY6": {  
                     regs.V[inst.x]  >>= 1;
                     break;
                   }
      case "8XY7": {  
                     regs.V[inst.x]  = (byte)(regs.V[inst.y] -  regs.V[inst.x]);
                     break;
                   }
      case "8XYE": {  
                     regs.V[inst.x]  <<= 1;
                     break;
                   }
      case "9XY0": {
                     if (regs.V[inst.x] != regs.V[inst.y]) regs.pc+=2;
                     break;
                   }
      case "ANNN": {
                     regs.I = inst.operand;
                     break;
                   }
      case "BNNN": {
                     regs.pc = (short)(regs.V[0] + inst.operand);
                     break;
                   }
      case "CXNN": {
                     Random rnd = new Random();
                     byte rNum = (byte)rnd.Next(0, 255);
                     regs.V[inst.x] = (byte)(rNum & inst.operand);
                     break;
                   }
      case "DXYN": {
                     Draw(regs.V[inst.x], regs.V[inst.y], inst.operand);
                     break;
                   }
      case "EX9E": {
                     if (bus.Key() == regs.V[inst.x]) regs.pc+=2;
                     break;
                   }
      case "EXA1": {
                     if (bus.Key() != regs.V[inst.x]) regs.pc+=2;
                     break;
                   }
      case "FX07": {
                     regs.V[inst.x] = delayTimer.Time;
                     break;
                   }
      case "FX0A": {
                     regs.V[inst.x] = bus.Key();
                     break;
                   }
      case "FX15": {
                     delayTimer.Time = regs.V[inst.x];
                     break;
                   }
      case "FX18": {
                     soundTimer.Time = regs.V[inst.x];
                     break;
                   }
      case "FX1E": {
                     regs.I += regs.V[inst.x];
                     break;
                   }
      case "FX29": {
                     //I = sprite_addr[Vx]	
                     //Sets I to the location of the sprite for the character in VX(only consider the lowest nibble). 
                     //Characters 0-F (in hexadecimal) are represented by a 4x5 font.[23]
                     short c = (short)(regs.V[inst.x] & 0x0F);
                     short f = 0xFF;
                     c = (short)(f & c);
                     // Console.WriteLine($"{regs.V[inst.x]:X2} {inst.x:X2} Setting I: {c}");
                     regs.I = c;
                     break;
                   }
      case "FX33": /* BCD  */ break;
      case "FX55": {
                     // reg_dump
                     int startingI = regs.I;
                     for (int i = 0; i <= inst.x; i++) {
                       bus.Write((short)(startingI + i),  (byte)regs.V[i]);
                     }
                     break;
                   }
      case "FX65": {
                     // reg_load
                     int startingI = regs.I;
                     for (int i = 0; i <= inst.x; i++) {
                       regs.V[i] = bus.Read((short)(startingI + i));
                     }
                     break;
                   }
      default: Console.WriteLine($"{inst.type} {inst.op:X4} is not implemented"); break;
    }
  }
  public int Step() {
    // pc is incremented in decoder
    Instruction i = decoder.Decode(bus,ref regs.pc);
    Console.WriteLine($"PC:${regs.pc:X4} OP:${i.op:X4} Type:{i.type} {i.c}");
    Execute(i, ref regs);
   return 1;
  }
}



public class Program
{
  public static void Main(string[] args) {
    String path = "./Soccer.ch8";    
    byte[] rom = File.ReadAllBytes(path);
    IBus bus = new Bus(rom);
    ICpu cpu = new Cpu(bus);

    for (int i = 0; i < 100; i++)
    cpu.Step();
  }
}
} 
