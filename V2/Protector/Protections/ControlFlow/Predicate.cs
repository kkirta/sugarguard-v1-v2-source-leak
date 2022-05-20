using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.ControlFlow
{
    internal interface IPredicate
    {
        void Init(CilBody body);
        void EmitSwitchLoad(IList<Instruction> instrs);
        int GetSwitchKey(int key);
    }

    internal class Predicate : IPredicate
    {
        readonly SugarLib ctx;
        bool inited;
        int xorKey;

        public Predicate(SugarLib ctx)
        {
            this.ctx = ctx;
        }

        public void Init(CilBody body)
        {
            if (inited)
                return;

            xorKey = new Random().Next();
            inited = true;
        }

        public void EmitSwitchLoad(IList<Instruction> instrs)
        {
            instrs.Add(Instruction.Create(OpCodes.Ldc_I4, xorKey));
            instrs.Add(Instruction.Create(OpCodes.Xor));
        }

        public int GetSwitchKey(int key)
        {
            return key ^ xorKey;
        }
    }
}
