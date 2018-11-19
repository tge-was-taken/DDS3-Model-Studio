using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Modeling.Utilities;
using DDS3ModelLibrary.Primitives;

namespace DDS3ModelLibrary
{
    public class Node : IBinarySerializable
    {
        // For debugging only, only valid when read from a file.
        private int mIndex;
        private int mParentIndex;

        private Vector3   mPosition;
        private Vector3   mRotation;
        private Vector3   mScale;
        private Matrix4x4 mLocalTransform;
        private Matrix4x4 mWorldTransform;
        private Node mParent;
        private bool mLocalTransformDirty;
        private bool mWorldTransformDirty;
        private bool mPRSDirty;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// Usually 1, but in hansya01.MB it is 0.
        /// </summary>
        public int Field00 { get; set; }

        public int Field04 { get; set; }

        public Node Parent
        {
            get => mParent;
            set
            {
                if ( mParent != value )
                {
                    mParent = value;
                    mWorldTransformDirty = true;
                }
            }
        }

        public Vector3 Rotation
        {
            get
            {
                if ( mPRSDirty )
                    UpdatePRS();

                return mRotation;
            }
            set
            {
                if ( mRotation != value )
                {
                    mRotation = value;
                    mLocalTransformDirty = true;
                }
            }
        }

        public Vector3 Position
        {
            get
            {
                if ( mPRSDirty )
                    UpdatePRS();

                return mPosition;
            }
            set
            {
                if ( mPosition != value )
                {
                    mPosition = value;
                    mLocalTransformDirty = true;
                }
            }
        }

        public Vector3 Scale
        {
            get
            {
                if ( mPRSDirty )
                    UpdatePRS();

                return mScale;
            }
            set
            {
                if ( mScale != value )
                {
                    mScale = value;
                    mLocalTransformDirty = true;
                }
            }
        }

        public BoundingBox BoundingBox { get; set; }

        public Geometry Geometry { get; set; }

        public int Field48 { get; set; }

        /// <summary>
        /// This is only used in a handful of really old models, use the <see cref="Geometry"/> property instead.
        /// </summary>
        /// <remarks>
        /// Used in shadow.MB
        /// </remarks>
        public MeshList DeprecatedMeshList { get; set; }

        /// <summary>
        /// This is only used in a handful of really old models, use the <see cref="Geometry"/> property instead.
        /// </summary>
        /// <remarks>
        /// Used in hansya01.MB and kyusyu01.MB.
        /// </remarks>
        public MeshList DeprecatedMeshList2 { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Gets the local transform of this node.
        /// </summary>
        public Matrix4x4 Transform
        {
            get
            {
                if ( mLocalTransformDirty )
                    UpdateLocalTransform();

                return mLocalTransform;
            }
            set
            {
                if ( mLocalTransform != value )
                {
                    mLocalTransform = value;
                    mPRSDirty = mWorldTransformDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets the world transform of this node.
        /// </summary>
        public Matrix4x4 WorldTransform
        {
            get
            {
                if ( mWorldTransformDirty )
                    UpdateWorldTransform();

                return mWorldTransform;
            }
        }

        public Node()
        {
            Field00     = 1;
            Field04     = 0;
            Parent      = null;
            mRotation    = Vector3.Zero;
            mPosition    = Vector3.Zero;
            mScale       = Vector3.One;
            BoundingBox = null;
            Geometry    = null;
            Field48     = 0;
            mLocalTransform = mWorldTransform = Matrix4x4.Identity;
        }

        private void UpdateLocalTransform()
        {
            mLocalTransform = Matrix4x4.CreateRotationX( Rotation.X ) * Matrix4x4.CreateRotationY( Rotation.Y ) *
                      Matrix4x4.CreateRotationZ( Rotation.Z );

            mLocalTransform *= Matrix4x4.CreateScale( Scale );
            mLocalTransform.Translation =  Position;
            mLocalTransformDirty = false;
        }

        private void UpdateWorldTransform()
        {
            mWorldTransform = Transform;
            if ( Parent != null )
                mWorldTransform *= Parent.WorldTransform;

            mWorldTransformDirty = false;
        }

        private void UpdatePRS()
        {
            Matrix4x4.Decompose( mLocalTransform, out var scale, out var rotation, out var translation );
            mPosition = translation;
            mRotation = rotation.ToEulerAngles();
            mScale    = scale;
            mPRSDirty = false;
        }

        public override string ToString()
        {
            return Name;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var nodes = context as List<Node> ?? throw new InvalidOperationException( "Expected context argument to be the node list" );
            Field00 = reader.ReadInt32();
            Field04 = reader.ReadInt32Expects( 0, "Node Field04 isnt 0" );
            mIndex = reader.ReadInt32();

            mParentIndex = reader.ReadInt32();
            if ( mParentIndex != -1 )
                Parent = nodes[ mParentIndex ];

            Rotation = reader.ReadVector3();
            reader.ReadSingleExpects( 0f, "Node Rotation W isnt 0" );
            Position = reader.ReadVector3();
            reader.ReadSingleExpects( 1f, "Node Position W isnt 1" );
            Scale = reader.ReadVector3();
            reader.ReadSingleExpects( 0f, "Node Scale W isnt 0" );

            BoundingBox = reader.ReadObjectOffset<BoundingBox>();
            var geometryOffset = reader.ReadInt32();

            if ( geometryOffset != 0 )
            {
                if ( BoundingBox != null )
                    Geometry = reader.ReadObjectAtOffset<Geometry>( geometryOffset );
                else
                    DeprecatedMeshList = reader.ReadObjectAtOffset<MeshList>( geometryOffset );
            }

            Field48 = reader.ReadInt32Expects( 0, "Node Field48 isnt 0" );
            DeprecatedMeshList2 = reader.ReadObjectOffset<MeshList>();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            ( int index, List<Node> nodes ) = ( (int, List<Node>) ) context;
            writer.Write( Field00 );
            writer.Write( Field04 );
            writer.Write( index );
            writer.Write( Parent == null ? -1 : nodes.IndexOf( Parent ) );
            writer.Write( Rotation );
            writer.Write( 0f );
            writer.Write( Position );
            writer.Write( 1f );
            writer.Write( Scale );
            writer.Write( 0f );
            writer.ScheduleWriteObjectOffset( BoundingBox, 16 );

            if ( Geometry != null )
                writer.ScheduleWriteObjectOffset( Geometry, 16 );
            else if ( DeprecatedMeshList != null )
                writer.ScheduleWriteObjectOffset( DeprecatedMeshList, 16 );
            else
                writer.Write( 0 );

            writer.Write( Field48 );
            writer.ScheduleWriteObjectOffset( DeprecatedMeshList2, 16 );
        }
    }
}