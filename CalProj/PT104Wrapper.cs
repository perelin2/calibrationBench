using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace CalProj
{
 

    class PT104Wrapper
    {

        [DllImport("PT10432.DLL")]
        public static extern short pt104_open_unit(ushort port);

        [DllImport("PT10432.DLL")]
        public static extern void pt104_close_unit(ushort port);

        [DllImport("PT10432.DLL")]
        public static extern ushort pt104_get_cycle(ref ulong cycle, ushort port);

        [DllImport("PT10432.DLL")]
        public static extern short pt104_set_channel(ushort port, ushort channel, ushort data_type, ushort no_of_wires);

        [DllImport("PT10432.DLL")]
        public static extern short pt104_get_value(ref long data, ushort port, ushort channel, ushort filtered);

    }
}
