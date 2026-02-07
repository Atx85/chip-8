using System;
using System.IO;
using System.Collections.Generic;
namespace Chip8 {
interface IBus { void Write(short addr, byte val); byte Read(short addr); }
interface ICpu { int Step(); }
interface IInstructionDecoder { void Decode(short opcode, ref short pc); }
class Bus : IBus {
  private byte[] memory; 

  public Bus(byte[] rom) {
    memory = new byte[0x1000];
    for (int i = 0; i < rom.Length; i++) {
      memory[0x200 + i] = rom[i];
    }
  }

  public byte Read(short addr) =>  memory[addr];
  public void Write(short addr, byte val) => memory[addr] = val;
}

public struct Instruction {
  public short op; 
  public string type;
  public string c;
  public short operand;
}

class Registers {
  public byte[] V;
  public short I;

  public Registers () {
    V = new byte[16];
  }
}
class Cpu : ICpu {
  private IBus bus;
  private short pc;
  private InstructionDecoder decoder;
  private Registers regs;

  public Cpu(IBus paramBus) {
    pc = 0x200;
    bus = paramBus;
    decoder = new InstructionDecoder();
    regs = new Registers();
  }
  public int Step() {
    Instruction i = decoder.Decode(bus,ref pc);
    Console.WriteLine($"${i.op:X4} - {i.c}");
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

    for (int i = 0; i < 20; i++)
    cpu.Step();
  }
}
} 
