using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Class
{
    public class MutationHelper
    {

        public static bool CanObfuscate(IList<Instruction> instructions, int i)
        {
            if (instructions[i + 1].GetOperand() != null)
                if (instructions[i + 1].Operand.ToString().Contains("bool"))
                    return false;
            if (instructions[i + 1].GetOpCode() == OpCodes.Newobj) return false;
            if (instructions[i].GetLdcI4Value() == 0 || instructions[i].GetLdcI4Value() == 1)
                return false;

            return true;
        }
        public class Block
        {
            public Block()
            {
                Instructions = new List<Instruction>();
            }
            public List<Instruction> Instructions { get; set; }

            public int Number { get; set; }
            public int Next { get; set; }
        }

        public static List<Block> ParseMethod(MethodDef method)
        {
            List<Block> blocks = new List<Block>();
            List<Instruction> body = new List<Instruction>(method.Body.Instructions);

            //splitting into blocks (Thanks to CodeOfDark#6320)
            Block block = new Block();
            int Id = 0;
            int usage = 0;
            block.Number = Id;
            block.Instructions.Add(Instruction.Create(OpCodes.Nop));
            blocks.Add(block);
            block = new Block();
            Stack<ExceptionHandler> handlers = new Stack<ExceptionHandler>();
            foreach (Instruction instruction in method.Body.Instructions)
            {
                foreach (var eh in method.Body.ExceptionHandlers)
                {
                    if (eh.HandlerStart == instruction || eh.TryStart == instruction || eh.FilterStart == instruction)
                        handlers.Push(eh);
                }
                foreach (var eh in method.Body.ExceptionHandlers)
                {
                    if (eh.HandlerEnd == instruction || eh.TryEnd == instruction)
                        handlers.Pop();
                }
                int stacks, pops;
                instruction.CalculateStackUsage(out stacks, out pops);
                block.Instructions.Add(instruction);
                usage += stacks - pops;
                if (stacks == 0)
                {
                    if (instruction.OpCode != OpCodes.Nop)
                    {
                        if ((usage == 0 || instruction.OpCode == OpCodes.Ret) && handlers.Count == 0)
                        {

                            block.Number = ++Id;
                            blocks.Add(block);
                            block = new Block();
                        }
                    }
                }
            }

            return blocks;
        }
    }
}
