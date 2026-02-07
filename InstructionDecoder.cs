using System;
using System.IO;
using System.Collections.Generic;

namespace Chip8 {
  class InstructionDecoder {
    private Dictionary<byte, Instruction> table;
    public InstructionDecoder() {
      table = new Dictionary<byte, Instruction>();
      Seed();
    }
    public Instruction Decode(IBus bus, ref short pc) {
      byte op = bus.Read(pc);
      pc++;
      byte op2 = bus.Read(pc);
      pc++;
      short opCode = (short)(op << 8 | op2);
      byte d0 = (byte)((opCode & 0xF000) >> 12);
      byte d1 = (byte)((opCode & 0x0F00) >> 8);
      byte d2 = (byte)((opCode & 0x00F0) >> 4);
      byte d3 = (byte)((opCode & 0x000F) >> 0);
      short nnn = (short)(opCode & 0x0FFF);
      short nn = (short)(opCode & 0x00FF);
      short n = (short)(opCode & 0x000F);

      Instruction i = new Instruction();
      i.type = "?";
      i.c = "";
      i.op = opCode;
      switch (d0) {
        case 0 : {
                   if (op << 6 == 0) {
                     i.type = "00E0";
                     i.c = "disp_clear();";
                   }
                   else {
                     i.type = "00EE";
                     i.c = "return;";
                   }

                   // there might be a 0NNN -> calls machine coderoutine at nnn;
                   break;
                 }
        case 1: {
                  i.type = "1NNN"; 
                  i.operand = nnn; 
                  i.c = $"jump {nnn:X4};";
                  break;
                }
        case 2: {
                  i.type = "2NNN"; 
                  i.operand = nnn;
                  i.c =$"call subroutine {nnn:X4};";
                  break;
                }
        case 3: {
                  i.type = "3XNN"; 
                  i.operand = nn;
                  i.c = $"if vx != {nn:X2}";
                  break;
                }
        case 4: {
                  i.type = "4XNN"; 
                  i.operand = nn;
                  i.c = $"if vx == {nn:X2};";
                  break;
                }
        case 5: {
                  i.type = "5XYN"; 
                  i.c = $"if vx != vy;";
                  break;
                }
        case 6: {
                  i.type = "6XNN"; 
                  i.operand = nn;
                  i.c = $"vx := {nn:X2};";
                  break;
                }
        case 7: {
                  i.type = "7XNN"; 
                  i.operand = nn;
                  i.c = $"vx += {nn:X2};";
                  break;
                }
        case 8: {
                  /*
                     8XY0 vx := vy
                     8XY1 vx |= vy Bitwise OR
                     8XY2 vx &= vy Bitwise AND
                     8XY3 vx ^= vy Bitwise XOR
                     8XY4 vx += vy vf = 1 on carry
                     8XY5 vx -= vy vf = 0 on borrow
                     8XY6 vx >>= vy vf = old least significant bit
                     8XY7 vx =- vy vf = 0 on borrow
                     8XYE vx <<= vy vf = old most significant bit
                   * */
                  switch (d3) {
                    case 0: i.c = $"vx := vy;"; i.type = "8XY0"; break;
                    case 1: i.c = $"vx |= vy;"; i.type = "8XY1"; break;
                    case 2: i.c = $"vx &= vy;"; i.type = "8XY2"; break;
                    case 3: i.c = $"vx ^= vy;"; i.type = "8XY3"; break;
                    case 4: i.c = $"vx += vy;"; i.type = "8XY4"; break;
                    case 5: i.c = $"vx -= vy;"; i.type = "8XY5"; break;
                    case 6: i.c = $"vx >>= vy;"; i.type = "8XY6"; break;
                    case 7: i.c = $"vx =- vy;"; i.type = "8XY7"; break;
                    case 0xE: i.c = $"vx <<= vy;"; i.type = "8XYE"; break;
                  }
                  break;
                }
        case 9: {
                  i.type = "9XY0";
                  i.c = $"if vx == vy then";
                  break;
                }
        case 0xA: {
                    i.type = "ANNN";
                    i.operand = nnn;
                    i.c = $"i := {nnn:X4};";
                    break;
                  }
        case 0xB: {
                    i.type = "BNNN";
                    i.operand = nnn;
                    i.c = $"jump ({nnn:X4} + v0)";
                    break;
                  }
        case 0xC: {
                    i.type = "CXNN";
                    i.operand = nn;
                    i.c = $"vx := random(0, 255) AND {nn:X2}";
                    break;
                  }
        case 0xD: {
                    i.type = "DXYN";
                    i.operand = n;
                    i.c = $"sprite vx vy {n:X1} vf = 1 on collision;";
                    break;
                  }
        case 0xE: {
                    switch (d3) {
                      case 0xE: {
                                  i.type = "EX9E";
                                  i.c = "if vx -key then Is a key not pressed?";
                                  break;
                                }
                      case 1: {
                                i.type = "EXA1";
                                i.c = "if vx key then Is a key pressed?";
                                break;
                              }
                    }
                    break;
                  }
        case 0xF: {
                    /*

                       FX07 vx := delay
                       FX0A vx := key Wait for a keypress
                       FX15 delay := vx
                       FX18 buzzer := vx
                       FX1E i += vx
                       FX29 i := hex vx Set i to a hex character
                       FX33 bcd vx Decode vx into binary-coded decimal
                       FX55 save vx Save v0-vx to i through (i+x)
                       FX65 load vx Load v0-vx from i through (i+x)
                       */
                    byte dd = (byte)(opCode & 0x00FF);
                    i.type = $"FX{dd:X2}";
                    switch (dd) {
                      case 0x07: { i.c = "vx := delay"; break;}
                      case 0x0A: { i.c = "vx := key"; break;}
                      case 0x15: { i.c = "delay := vx"; break;}
                      case 0x18: { i.c = "buzzer := vx"; break;}
                      case 0x1E: { i.c = "i += vx;"; break;}
                      case 0x29: { i.c = "i := hex;"; break;}
                      case 0x33: { i.c = "bcd vx"; break; }
                      case 0x55: { i.c = "savd vx;"; break;}
                      case 0x65: { i.c = "load vx;"; break;}
                    }
                    break;
                  }
      }
      return i;
    }
    public void Seed() {
    }
  }
}
