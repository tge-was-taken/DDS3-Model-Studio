using System.Collections.Generic;
using System.Linq;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class Geometry : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short Field02 { get; set; }

        public List<Mesh> Meshes { get; }

        public Geometry()
        {
            Meshes = new List<Mesh>();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            reader.ReadOffset( () =>
            {
                var meshCount = reader.ReadInt16();
                Field02 = reader.ReadInt16();

                for ( int i = 0; i < meshCount; i++ )
                {
                    reader.ReadOffset( () =>
                    {
                        var meshType = ( MeshType ) reader.ReadInt32();
                        Mesh mesh = null;

                        switch ( meshType )
                        {
                            case MeshType.Type1:
                                break;
                            case MeshType.Type2:
                                break;
                            case MeshType.Type3:
                                break;
                            case MeshType.Type4:
                                break;
                            case MeshType.Type5:
                                break;
                            case MeshType.Type7:
                                mesh = reader.ReadObject<MeshType7>();
                                break;
                            case MeshType.Type8:
                                break;

                            default:
                                throw new UnexpectedDataException( $"Unknown mesh type: {meshType}" );
                        }

                        if ( mesh != null )
                            Meshes.Add( mesh );
                    });
                }
            } );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var canWrite = Meshes?.Count > 0 && Meshes.All( x => x.Type == MeshType.Type7 );

            if ( canWrite )
            {
                writer.ScheduleWriteOffsetAligned( 16, () =>
                {
                    writer.Write( (short)Meshes.Count );
                    writer.Write( Field02 );

                    foreach ( var mesh in Meshes )
                    {
                        writer.ScheduleWriteOffsetAligned( 16, () =>
                        {
                            writer.Write( ( int ) mesh.Type );
                            writer.WriteObject( mesh );
                        });                
                    }
                });
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

    public class MeshType8
    {

    }

    public class Shape
    {

    }
}