
using SugarGuard.Helpers.MethodBlocks;

using dnlib.DotNet.Emit;
using System.Collections.Generic;
using SugarGuard.Core;
using SugarGuard.Helpers.Emulator;

namespace SugarGuard.Protections.Mutation
{
    public static class BlockHandler
    {
        public static Block GenerateBlock(Int32Local local, BlockType type)
        {
            var instructions = new List<Instruction>();

            switch (type)
            {
                case BlockType.Arithmethic:
                    instructions.Add(OpCodes.Nop.ToInstruction());
                    instructions.Add(OpCodes.Ldloc_S.ToInstruction(local.Local));
                    instructions.Add(OpCodes.Ldc_I4.ToInstruction(Utils.RandomInt32()));
                    instructions.Add(Utils.GetCode().ToOpCode().ToInstruction());
                    instructions.Add(OpCodes.Stloc_S.ToInstruction(local.Local));
                    break;
            }

            local.Value = Emulate(instructions, local);

            return new Block(instructions.ToArray(), false, true, false, local.Value);
        }

        public static int Emulate(List<Instruction> instructions, Int32Local local)
        {
            var emu = new Emulator(instructions, new List<Local> { local.Local });
            emu._context.SetLocalValue(local.Local, local.Value);
            return emu.Emulate();
        }

        public static List<Instruction> GenerateBranch(int value, Local local, Instruction target)
        {
            bool hasElse = Utils.RandomBoolean();
            switch (Utils.rnd.Next(0, 1))
            {
                case 0:
                    return GenerateBrFalse(value, local, target, hasElse);

            }
            return null;
        }

        public static List<Instruction> GenerateBrFalse(int value, Local local, Instruction target, bool fake)
        {
            var newValue = Utils.RandomInt32();
            var code = Utils.GetCode(true);
            var r = Calculate(value, newValue, code, false);

            var result = new List<Instruction>();
            var nopTarget = OpCodes.Nop.ToInstruction();
            result.Add(OpCodes.Ldloc_S.ToInstruction(local));
            result.Add(OpCodes.Ldc_I4.ToInstruction(newValue));
            result.Add(code.ToOpCode().ToInstruction());
            result.Add(OpCodes.Ldc_I4.ToInstruction(r));
            result.Add(OpCodes.Ceq.ToInstruction());
            result.Add(OpCodes.Brfalse_S.ToInstruction(nopTarget));
            result.Add(OpCodes.Br.ToInstruction(target));
            result.Add(nopTarget);
            return result;
        }

        public static List<Instruction> GenerateBrTrue(int value, Local local, Instruction target, bool fake)
        {
            var newValue = Utils.RandomInt32();
            var code = Utils.GetCode(true);
            var r = Calculate(value, newValue, code, false);

            var result = new List<Instruction>();
            var brTarget = OpCodes.Br.ToInstruction(target);
            result.Add(OpCodes.Ldloc_S.ToInstruction(local));
            result.Add(OpCodes.Ldc_I4.ToInstruction(newValue));
            result.Add(code.ToOpCode().ToInstruction());
            result.Add(OpCodes.Ldc_I4.ToInstruction(r));
            result.Add(OpCodes.Ceq.ToInstruction());
            result.Add(OpCodes.Brtrue_S.ToInstruction(brTarget));

            result.Add(brTarget);
            return result;
        }

        public static Block GenerateBrBlock(Instruction target)
        {
            var result = new List<Instruction>();
            result.Add(OpCodes.Br.ToInstruction(target));
            return new Block(result.ToArray(), false, true, true);
        }
        public static int Calculate(int n1, int n2, Code code, bool reverse = true)
        {
            switch (code)
            {
                case Code.Add:
                    return reverse ? n1 - n2 : n1 + n2;
                case Code.Sub:
                    return reverse ? n1 + n2 : n1 - n2;
                case Code.Xor:
                    return n1 ^ n2;
            }
            return 0;

        }

    }
}
