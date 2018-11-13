using System.Linq;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class Geometry : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public MeshList Meshes { get; private set; }

        public Geometry()
        {
            Meshes = new MeshList();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Meshes = reader.ReadObjectOffset<MeshList>();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var canWrite = Meshes?.Count > 0 && Meshes.All( x => x.Type == MeshType.Type7 || x.Type == MeshType.Type8 );

            if ( canWrite )
            {
                writer.ScheduleWriteOffsetAligned( 16, () => { writer.WriteObject( Meshes ); } );
            }
            else
            {
                writer.Write( 0 );
            }
        }
    }

    public class MeshType1
    {
    }

    public class MeshType2
    {
    }

    public class MeshType3
    {

    }

    public class MeshType4
    {
    }

    public class MeshType5
    {

    }

    public class Shape
    {

    }
}