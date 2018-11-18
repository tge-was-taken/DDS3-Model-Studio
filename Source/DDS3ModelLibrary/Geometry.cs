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
            var canWrite = Meshes?.Count > 0 && Meshes.All( x => x.Type == MeshType.Type1 || x.Type == MeshType.Type2 || x.Type == MeshType.Type5 || x.Type == MeshType.Type7 || x.Type == MeshType.Type8 );

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
}