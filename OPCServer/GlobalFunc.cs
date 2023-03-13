using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPCServer
{
    internal static class GlobalFunc
    {

        public static void Add<T>(T obj, ref T[] objs)
        {
            if (objs == null)
                objs = new T[] { obj };
            else
            {
                T[] a = new T[objs.Length + 1];
                objs.CopyTo(a, 0);
                a[a.Length - 1] = obj;
                objs = a;
            }
        }

        public enum DataTypeS : byte
        {
            DTUInt = 0,
            DTFloat = 1,
            DTString = 2,
            DTBool = 3
        }
    }
}
