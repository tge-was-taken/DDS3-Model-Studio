#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Numerics;

namespace DDS3ModelLibrary::Models::Conversion
{
	using namespace Textures;
	using namespace Materials;

	public ref class FbxModelExporterConfig
	{
	public:
		property bool ExportMultipleUvLayers;

		inline FbxModelExporterConfig()
		{
			ExportMultipleUvLayers = true;
		}
	};

	template<typename T>
	ref struct Ptr
	{
	public:
		Ptr( T* ptr ) { mPtr = ptr; }
		T& operator*() { return *mPtr; }
		T* Get() { return mPtr; }
	private:
		T* mPtr;
	};


	ref class MorphData
	{
	public:
		List<Vector3> Vertices;
		List<Vector3> Normals;
	};

	// Intermediate structure for meshes
	ref class MeshData
	{
	public:
		inline MeshData( MeshType type )
		{
			MeshType = type;
		}

		MeshType MeshType;
		Node^ ParentNode;
		List<Vector3> Vertices;
		List<Vector3> Normals;
		List<Color> Colors;
		List<Vector2> UV1;
		List<Vector2> UV2;
		List<NodeWeight> Weights;
		List<Triangle> Triangles;
		int MaterialIndex;
		List<MorphData^> MorphData;
	};

	public ref class FbxModelExporter sealed : public ModelExporter<FbxModelExporter^, FbxModelExporterConfig^>
	{
	public:
		FbxModelExporter();
		~FbxModelExporter();

		void Export( Model^ model, String^ path, FbxModelExporterConfig^ config, TexturePack^ textures ) override;

	private:
		void Reset();
		FbxScene* ConvertModelToFbxScene( Model^ model, TexturePack^ textures );
		void ConvertMaterialToFbxSurfacePhong( fbxsdk::FbxScene* fScene, DDS3ModelLibrary::Models::Model^ model, DDS3ModelLibrary::Materials::Material^ mat, DDS3ModelLibrary::Textures::TexturePack^ textures, const size_t& i );
		FbxNode* ConvertNodeToFbxNode( Model^ model, Node^ node, FbxScene* fScene, int index );
		void PopulateConvertedFbxNode( Model^ model, Node^ node, FbxScene* fScene, FbxNode* fNode );
		FbxDouble3 ConvertNumericsVector3ToFbxDouble3( Vector3 value );
		FbxDouble3 ConvertNumericsVector3RotationToFbxDouble3( Vector3 rotation );
		void ConvertMeshListToFbxNodes( Model^ model, Node^ node, MeshList^ meshList, FbxScene* fScene, FbxNode* fNode );
		FbxNode* CreateFbxNodeForMesh( Model^ model, Node^ node, const char* name, FbxScene* fScene );
		FbxMesh* ConvertMeshToFbxMesh( Model^ model, Node^ node, Mesh^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode );
		FbxMesh* ConvertMeshType1ToFbxMesh( Model^ model, Node^ node, MeshType1^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode );
		FbxMesh* ConvertMeshType2ToFbxMesh( Model^ model, Node^ node, MeshType2^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode );
		FbxMesh* ConvertMeshType4ToFbxMesh( Model^ model, Node^ node, MeshType4^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode );
		FbxMesh* ConvertMeshType5ToFbxMesh( Model^ model, Node^ node, MeshType5^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode );
		FbxMesh* ConvertMeshType7ToFbxMesh( Model^ model, Node^ node, MeshType7^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode );
		FbxMesh* ConvertMeshType8ToFbxMesh( Model^ model, Node^ node, MeshType8^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode );
		void ConvertPositionsToFbxControlPoints( FbxVector4** fControlPoints, array<Vector3>^ positions );
		void ConvertNormalsToFbxLayerElementNormalDirectArray( FbxLayerElementNormal* fElementNormal, array<Vector3>^ normals, int vertexStart );
		void ConvertTexCoordsToFbxLayerElementUV( FbxMesh* fMesh, const char* name, array<Vector2>^ texCoords, int layer );
		void ConvertTexCoordsToFbxLayerElementUVDirectArray( FbxLayerElementUV* fElementUV, array<Vector2>^ texCoords, int vertexStart );
		void ConvertColorsToFbxLayerElementVertexColorsDirectArray( FbxLayerElementVertexColor* fElementColors, array<Color>^ colors, int vertexStart );
		void ConvertTrianglesToFbxPolygons( FbxMesh* fMesh, array<Triangle>^ triangles, int vertexStart );
		FbxAMatrix ConvertNumericsMatrix4x4ToFbxAMatrix( Matrix4x4& m );
		void ConvertNodeWeightsToFbxClusters( array<array<DDS3ModelLibrary::Models::NodeWeight>^>^ weights, System::Collections::Generic::Dictionary<int, 
			System::IntPtr>^ fClusterLookup, fbxsdk::FbxScene* fScene, FbxNode* fMeshNode, fbxsdk::FbxSkin* fSkin, int vertexStart, Model^ model );

		String^ FormatNodeName( Model^ model, Node^ node );
		String^ FormatMaterialName( Model^ model, Material^ material );
		String^ FormatTextureName( TexturePack^ textures, Texture^ texture, int index );

		FbxLayer* GetFbxMeshLayer( fbxsdk::FbxMesh* fMesh, int layer );
		FbxGeometryElementNormal* CreateFbxMeshElementNormal( fbxsdk::FbxMesh* fMesh );
		FbxGeometryElementMaterial* CreateFbxElementMaterial( fbxsdk::FbxMesh* fMesh );
		FbxGeometryElementUV* CreateFbxMeshElementUV( fbxsdk::FbxMesh* fMesh, const char* name, int layer );
		FbxGeometryElementVertexColor* CreateFbxMeshElementVertexColor( fbxsdk::FbxMesh* fMesh );
		void CreateFbxSkinForRigidParentNodeBinding( fbxsdk::FbxScene* fScene, fbxsdk::FbxNode* fNode, fbxsdk::FbxMesh* fMesh );
		void ExportFbxScene( FbxScene* scene, String^ path );

		FbxManager* mManager;
		Dictionary<Node^, IntPtr>^ mNodeToFbxNodeLookup;
		List<IntPtr>^ mConvertedNodes;
		Dictionary<int, IntPtr>^ mMaterialCache;
		Dictionary<int, IntPtr>^ mTextureCache;
		FbxModelExporterConfig^ mConfig;
	};
}
