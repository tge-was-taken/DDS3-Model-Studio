using Assimp;
using DDS3ModelLibrary.IO.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DDS3ModelLibrary.Models
{
    public class Geometry : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public MeshList[] MeshLists { get; } = new MeshList[3];

        public MeshList Meshes
        {
            get => MeshLists[0];
            set => MeshLists[0] = value;
        }

        public MeshList TranslucentMeshes
        {
            get => MeshLists[1];
            set => MeshLists[1] = value;
        }

        public MeshList MeshList3
        {
            get => MeshLists[2];
            set => MeshLists[2] = value;
        }

        public Geometry()
        {
        }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            var meshList = reader.ReadObjectOffset<MeshList>();
            var meshListIndex = 0;
            while (meshList != null)
            {
                MeshLists[meshListIndex++] = meshList;
                meshList = reader.ReadObjectOffset<MeshList>();
            }
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            foreach (var meshList in MeshLists)
            {
                if (meshList is null)
                {
                    writer.WriteInt32(0);
                    break;
                }
                else
                {
                    writer.ScheduleWriteOffsetAligned(4, () => { writer.WriteObject(meshList); });
                }
            }
        }
    }
}