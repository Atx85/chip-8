using System;
using System.IO;
using System.Collections.Generic;
namespace Chip8 {
interface IBus { void Write(short addr, byte val); byte Read(short addr); }
interface ICpu { int Step(); }
interface IInstructionDecoder { void Decode(short opcode); }
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
  byte op; 
  string type;
}

class InstructionDecoder {
  private Dictionary<byte, Instruction> table;
  public InstructionDecoder() {
    table = new Dictionary<byte, Instruction>();
    Seed();
  }
  public Instruction Decode(IBus bus) {
    byte op = bus.Read(pc);
    pc++;
    byte op2 = bus.Read(pc);
    pc++;
    string opCode = $"{op:X2}{op2:X2}";
    Console.WriteLine(opCode);
  }
  public void Seed() {
  }
}

class Cpu : ICpu {
  private IBus bus;
  private short pc;
  private InstructionDecoder decoder;

  public Cpu(IBus paramBus) {
    pc = 0x200;
    bus = paramBus;
    decoder = new InstructionDecoder();
  }
  public int Step() {
    decoder.Decode(bus);
   return 1;
  }
}



public class Program
{
  public static void Main(string[] args) {
    Console.WriteLine("Hello World");
    String path = "./Soccer.ch8";    
    byte[] rom = File.ReadAllBytes(path);
    IBus bus = new Bus(rom);
    ICpu cpu = new Cpu(bus);

    for (int i = 0; i < 20; i++)
    cpu.Step();
  }
}
} 
