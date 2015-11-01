using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModPacker
{
    class MainClass
    {
        static void Main(string[] args)
        {
            MainClass mc = new MainClass();

            args = new string[] { @"D:\fs_build_1.55\", @"firststrike" };

            Packer.Run(args[0], args[1]);

            Console.ReadKey();
        }

        
    }
}
