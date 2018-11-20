using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Modeling.Utilities;

namespace DDS3ModelLibrary
{
    public class FieldObjectTransform : IBinarySerializable
    {
        private Vector3 mPosition;
        private Vector3 mRotation;
        private Vector3 mScale;
        private Matrix4x4 mMatrix;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public Vector3 Position
        {
            get => mPosition;
            set
            {
                if ( value != mPosition )
                {
                    mPosition = value;
                    UpdateMatrix();
                }
            }
        }

        public Vector3 Rotation
        {
            get => mRotation;
            set
            {
                if ( value != mRotation )
                {
                    mRotation = value;
                    UpdateMatrix();
                }
            }
        }

        public Vector3 Scale
        {
            get => mScale;
            set
            {
                if ( value != mScale )
                {
                    mScale = value;
                    UpdateMatrix();
                }
            }
        }

        public Matrix4x4 Matrix
        {
            get => mMatrix;
            set
            {
                if ( value != mMatrix )
                {
                    mMatrix = value;
                    UpdatePRS();
                }
            }
        }

        public FieldObjectTransform()
        {
            mMatrix = Matrix4x4.Identity;
        }

        public FieldObjectTransform( Vector3 position, Vector3 rotation, Vector3 scale )
        {
            mPosition = position;
            mRotation = rotation;
            mScale = scale;
            UpdateMatrix();
        }

        public FieldObjectTransform( Matrix4x4 matrix )
        {
            Matrix = matrix; 
        }

        private void UpdateMatrix()
        {
            mMatrix = Matrix4x4.CreateRotationX( Rotation.X ) * Matrix4x4.CreateRotationY( Rotation.Y ) *
                            Matrix4x4.CreateRotationZ( Rotation.Z );

            mMatrix *= Matrix4x4.CreateScale( Scale );
            mMatrix.Translation =  Position;
        }

        private void UpdatePRS()
        {
            Matrix4x4.Decompose( mMatrix, out var scale, out var rotation, out var translation );
            mPosition = translation;
            mRotation = rotation.ToEulerAngles();
            mScale    = scale;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Position = reader.ReadVector3();
            reader.SeekCurrent( 4 );

            Rotation = reader.ReadVector3();
            reader.SeekCurrent( 4 );

            Scale = reader.ReadVector3();
            reader.SeekCurrent( 4 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( Position );
            writer.Write( 1f );
            writer.Write( Rotation );
            writer.Write( 1f );
            writer.Write( Scale );
            writer.Write( 1f );
        }
    }
}