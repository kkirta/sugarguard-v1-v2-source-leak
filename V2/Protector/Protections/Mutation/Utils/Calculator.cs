using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Mutation
{
    public class Calculator
    {
        public static Random rnd = new Random();
        private OpCode cOpCode = null;
        private int result = 0;
        public Calculator(int value, int value2)
        {
            result = Calculate(value, value2);
        }

        public int getResult()
        {
            return result;
        }

        public OpCode getOpCode()
        {
            return cOpCode;
        }

        private int Calculate(int num, int num2)
        {
            int cresult = 0;
            int r = rnd.Next(0, 3);

            switch (r)
            {
                case 0:
                    cresult = num + num2;
                    cOpCode = OpCodes.Sub;
                    break;
                case 1:
                    cresult = num ^ num2;
                    cOpCode = OpCodes.Xor;
                    break;
                case 2:
                    cresult = num - num2;
                    cOpCode = OpCodes.Add;
                    break;
            }
            return cresult;
        }

    }
}
