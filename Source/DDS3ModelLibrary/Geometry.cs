using System;
using System.Linq;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class Geometry : IBinarySerializable
    {
        private MeshList mTranslucentMeshes;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public MeshList Meshes { get; set; }

        public MeshList TranslucentMeshes
        {
            get => mTranslucentMeshes;
            set
            {
                mTranslucentMeshes = value;
                if ( Meshes == null )
                    throw new InvalidOperationException( $"{nameof( TranslucentMeshes )} must be null if {nameof( Meshes )} is null" );
            }
        }

        public Geometry()
        {
            Meshes = new MeshList();
            TranslucentMeshes = null;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Meshes = reader.ReadObjectOffset<MeshList>();
            if ( Meshes != null )
                TranslucentMeshes = reader.ReadObjectOffset<MeshList>();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            void WriteMeshList(MeshList meshes)
            {
                var canWrite = meshes?.Count > 0 && meshes.All( x => x.Type == MeshType.Type1 || x.Type == MeshType.Type2 || x.Type == MeshType.Type4 ||
                                                                     x.Type == MeshType.Type5 || x.Type == MeshType.Type7 || x.Type == MeshType.Type8 );

                if ( canWrite )
                {
                    writer.ScheduleWriteOffsetAligned( 16, () => { writer.WriteObject( meshes ); } );
                }
                else
                {
                    writer.Write( 0 );
                }
            }

            WriteMeshList( Meshes );
            if ( Meshes != null )
                WriteMeshList( TranslucentMeshes );
        }
    }
}