using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class Model : Resource, IFieldObjectResource
    {
        public override ResourceDescriptor ResourceDescriptor { get; } = new ResourceDescriptor( ResourceFileType.Model, ResourceIdentifier.Model );

        FieldObjectResourceType IFieldObjectResource.FieldObjectResourceType => FieldObjectResourceType.Model;

        public List<Node> Nodes { get; }

        public List<Material> Materials { get; private set; }

        public List<ModelExtension> Extensions { get; }

        public Model()
        {
            Nodes = new List<Node>();
            Materials = new List<Material>();
            Extensions = new List<ModelExtension>();
        }

        internal override void ReadContent( EndianBinaryReader reader, IOContext context )
        {
            var relocationTableOffset = reader.ReadInt32();
            var relocationTableSize = reader.ReadInt32();

            for ( int i = 0; i < 2; i++ )
                reader.ReadUInt32Expects( 0, "Model header padding is not 0" );

            // Hacky fix for field models
            if ( !context.IsFieldObject )
                reader.PushBaseOffset();

            reader.ReadOffset( () =>
            {
                var nodeCount = reader.ReadInt32();
                reader.Align( 16 );

                for ( int i = 0; i < nodeCount; i++ )
                    Nodes.Add( reader.ReadObject<Node>( Nodes ) );
            } );
            reader.ReadOffset( () =>
            {
                var materialCount = reader.ReadInt32();
                Materials = reader.ReadObjectList<Material>( materialCount );
            } );
            var morpherMeshCount = reader.ReadInt32();
            reader.ReadOffset( () =>
            {
                while ( true )
                {
                    var extensionStart = reader.Position;
                    var extensionHeader = reader.ReadObject<ModelExtensionHeader>();
                    if ( extensionHeader.Identifier == 0 )
                        break;

                    var extensionEnd = extensionStart + extensionHeader.Size;

                    switch ( extensionHeader.Identifier )
                    {
                        case ModelExtensionIdentifier.NodeName:
                            // Special case as we move the data into the nodes themselves
                            for ( int i = 0; i < Nodes.Count; i++ )
                            {
                                var nodeName = reader.ReadString( StringBinaryFormat.NullTerminated );
                                reader.Align( 4 );
                                var nodeId = reader.ReadInt32();
                                Nodes[nodeId].Name = nodeName;
                            }
                            break;
                        default:
                            Extensions.Add( reader.ReadObject<ModelBinaryExtension>( extensionHeader ) );
                            break;
                    }

                    reader.SeekBegin( extensionEnd );
                }
            } );

            if ( !context.IsFieldObject )
                reader.PopBaseOffset();
        }

        internal override void WriteContent( EndianBinaryWriter writer, IOContext context )
        {
            if ( !context.IsFieldObject )
            {
                // TODO: implement this properly
                writer.OffsetPositions.Clear();
            }

            var start = writer.Position;

            if ( !context.IsFieldObject )
            {
                // Relocation table needs this base offset
                writer.PushBaseOffset( start + 16 );

                // Write relocation table last (lowest priority)
                writer.ScheduleWriteOffsetAligned( -1, 16, () =>
                {
                    // Encode & write relocation table
                    var encodedRelocationTable =
                        RelocationTableEncoding.Encode( writer.OffsetPositions.Select( x => ( int ) x ).ToList(), ( int ) writer.BaseOffset );
                    writer.Write( encodedRelocationTable );

                    // Kind of a hack here, but we need to write the relocation table size after the offset
                    // Seeing as we have the offset positions required to encode the relocation table only at the very end
                    // I can't really think of a better solution
                    var end = writer.Position;
                    writer.SeekBegin( start + 4 );
                    writer.Write( encodedRelocationTable.Length );
                    writer.SeekBegin( end );
                } );
            }
            else
            {
                writer.Write( 0 );
                writer.Write( 0 );
            }

            writer.Align( 16 );

            writer.ScheduleWriteOffsetAligned( 16, () =>
            {
                writer.Write( Nodes.Count );
                writer.Align( 16 );
                for ( int i = 0; i < Nodes.Count; i++ )
                    writer.WriteObject( Nodes[ i ], ( i, Nodes ) );
            } );

            writer.ScheduleWriteOffsetAligned( 16, () =>
            {
                writer.Write( Materials.Count );
                for ( int i = 0; i < Materials.Count; i++ )
                    writer.WriteObject( Materials[ i ], i );
            } );

            var morpherMeshCount = Nodes.Where( x => x.Geometry != null && x.Geometry.Meshes?.Count > 0 )
                                        .Sum( x => x.Geometry.Meshes.Count( y => MeshTypeTraits.HasMorphers( y.Type ) ) );

            writer.Write( morpherMeshCount );

            writer.ScheduleWriteOffsetAligned( 16, () =>
            {
                // NDNM is a special case as we stored the data in the nodes themselves.
                bool noNodeNames = Nodes.All( x => x.Name == null );
                if ( !noNodeNames )
                {
                    WriteExtension( writer, ModelExtensionIdentifier.NodeName, () =>
                    {
                        for ( int i = 0; i < Nodes.Count; i++ )
                        {
                            writer.Write( Nodes[i].Name, StringBinaryFormat.NullTerminated );
                            writer.Align( 4 );
                            writer.Write( i );
                        }
                    } );
                }

                foreach ( var extension in Extensions )
                    WriteExtension( writer, extension.Identifier, () => writer.WriteObject( extension ) );

                // Write dummy end extension
                writer.Write( 0 );
                writer.Write( 0 );
                writer.Align( 16 );
            } );

            if ( !context.IsFieldObject )
            {
                // TODO(TGE): implement this properly
                writer.PerformScheduledWrites();
            }
        }

        private static void WriteExtension( EndianBinaryWriter writer, ModelExtensionIdentifier extensionIdentifier, Action writeAction )
        {
            // Write extension
            var extensionStart = writer.Position;
            writer.SeekCurrent( 8 );

            writeAction();

            writer.Align( 16 );
            var extensionEnd = writer.Position;

            // Calculate size and write extension header
            var extensionSize = extensionEnd - extensionStart;
            writer.SeekBegin( extensionStart );
            writer.Write( ( int )extensionIdentifier );
            writer.Write( ( int )extensionSize );

            // Seek back to the end of the extension
            writer.SeekBegin( extensionEnd );
        }
    }
}
