using System;
using System.Collections.Generic;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class ModelPackInfo : Resource
    {
        private const uint BOM = 0xFFFFFFFE;
        private const uint INFO_OFFSET = 0x14;

        public override ResourceDescriptor ResourceDescriptor { get; } =
            new ResourceDescriptor( ResourceFileType.Default, ResourceIdentifier.ModelPackInfo );

        public short Field1A { get; set; }
        public short Field22 { get; set; }
        public List<ModelPackEffectInfo> EffectInfos { get; private set; }

        public ModelPackInfo()
        {
            Field1A = 0;
            Field22 = 0;
            EffectInfos = new List<ModelPackEffectInfo>();
        }

        // IBinarySerializable
        internal override void ReadContent( EndianBinaryReader reader, IOContext context )
        {
            var bom = reader.ReadUInt32Expects( BOM, "Model pack info BOM value does not match expected value" );
            var infoOffset = reader.ReadUInt32Expects( INFO_OFFSET, "Model pack info info offset value does match expected value" );
            var modelCount = reader.ReadInt16();
            Field1A = reader.ReadInt16Expects( 0, "Model pack info Field1A value is not 0" );
            var effectInfoCount = reader.ReadInt16();
            var effectCount = reader.ReadInt16();
            var animationCount = reader.ReadInt16();
            Field22 = reader.ReadInt16Expects( 0, "Model pack info Field22 value is not 0" );
            EffectInfos = reader.ReadObjectList<ModelPackEffectInfo>( effectInfoCount );
        }

        internal override void WriteContent( EndianBinaryWriter writer, IOContext context )
        {
            if ( !( context.Context is ModelPack modelPack ) )
                throw new InvalidOperationException();

            writer.Write( BOM );
            writer.Write( INFO_OFFSET );
            writer.Write( ( short )modelPack.Models.Count );
            writer.Write( Field1A );
            writer.Write( ( short )EffectInfos.Count );
            writer.Write( ( short )modelPack.Effects.Count );
            writer.Write( ( short )modelPack.AnimationPacks.Count );
            writer.Write( Field22 );
            writer.WriteObjects( EffectInfos );
        }
    }
}