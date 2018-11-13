using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DDS3ModelLibrary;

namespace DDS3ModelLibraryCLI
{
    class Program
    {
        static void Main( string[] args )
        {
            var modelPack = new ModelPack( @"..\..\..\..\Resources\player_a.PB" );
            modelPack.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );
        }
    }
}
