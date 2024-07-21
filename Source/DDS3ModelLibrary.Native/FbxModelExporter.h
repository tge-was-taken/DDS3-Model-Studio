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
		property bool MergeMeshes;
		property bool ConvertBlendShapesToMeshes;

		inline FbxModelExporterConfig()
		{
			ExportMultipleUvLayers = true;
			MergeMeshes = true;
			ConvertBlendShapesToMeshes = true;
		}
	};


	// Intermediate structures for meshes
	value struct GenericBlendShape
	{
	public:
		array<Vector3>^ Vertices;
		array<Vector3>^ Normals;
	};

	value struct GenericPrimitiveGroup
	{
	public:
		int MaterialIndex;
		array<Triangle>^ Triangles;
	};

	ref class GenericMesh
	{
	public:
		Mesh^ Source;
		Node^ ParentNode;
		array<Vector3>^ Vertices;
		array<Vector3>^ Normals;
		array<Color>^ Colors;
		array<Vector2>^ UV1;
		array<Vector2>^ UV2;
		array<array<NodeWeight>^>^ Weights;
		array<GenericPrimitiveGroup>^ Groups;
		array<GenericBlendShape>^ BlendShapes;
	};

	public ref class MeshConversionContext
	{
	public:
		FbxMesh* Mesh;
		FbxVector4* ControlPoints;
		FbxGeometryElementNormal* ElementNormal;
		FbxGeometryElementMaterial* ElementMaterial;
		FbxGeometryElementVertexColor* ElementColor;
		FbxGeometryElementUV* ElementUV;
		FbxGeometryElementUV* ElementUV2;
		FbxSkin* Skin;
		Dictionary<int, IntPtr>^ ClusterLookup;
		FbxBlendShape* BlendShape;
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
		void ConvertBlendShapesToMeshes( System::Collections::Generic::List<DDS3ModelLibrary::Models::Conversion::GenericMesh^>^ meshes, DDS3ModelLibrary::Models::Model^ model );
		void MergeMeshes( System::Collections::Generic::List<DDS3ModelLibrary::Models::Conversion::GenericMesh^>^& meshes, DDS3ModelLibrary::Models::Model^ model );
		FbxNode* CreateFbxNodeForMesh( FbxScene* fScene, const char* name );
		void ConvertProcessedMeshToFbxMesh( Model^ model, GenericMesh^ mesh, MeshConversionContext^ work, int vertexStart );
		void AddRigidFbxClusterForParentNode( Model^ model, GenericMesh^ mesh, FbxSkin* fSkin, Dictionary<int, IntPtr>^ fClusterLookup, int vertexStart );
		void ConvertMaterialToFbxSurfacePhong( fbxsdk::FbxScene* fScene, DDS3ModelLibrary::Models::Model^ model, DDS3ModelLibrary::Materials::Material^ mat, DDS3ModelLibrary::Textures::TexturePack^ textures, const int& i );
		
		void BuildNodeToFbxNodeMapping( Model^ model, FbxScene* fScene );
		void ConvertNodeToFbxNode( Model^ model, Node^ node, FbxScene* fScene, FbxNode* fNode, List<GenericMesh^>^ processedMeshes );
		FbxDouble3 ConvertNumericsVector3ToFbxDouble3( Vector3 value );
		FbxDouble3 ConvertNumericsVector3RotationToFbxDouble3( Vector3 rotation );

		void ProcessMeshList( Model^ model, Node^ node, MeshList^ meshList, List<GenericMesh^>^ processedMeshes );
		FbxNode* CreateFbxNodeForMesh( Model^ model, Node^ node, const char* name, FbxScene* fScene );

		void ProcessMesh( Model^ model, Node^ node, Mesh^ mesh, List<GenericMesh^>^ processedMeshes );
		void ProcessMeshType1( Model^ model, Node^ node, MeshType1^ mesh, List<GenericMesh^>^ processedMeshes );
		void ProcessMeshType2( Model^ model, Node^ node, MeshType2^ mesh, List<GenericMesh^>^ processedMeshes );
		void ProcessMeshType4( Model^ model, Node^ node, MeshType4^ mesh, List<GenericMesh^>^ processedMeshes );
		void ProcessMeshType5( Model^ model, Node^ node, MeshType5^ mesh, List<GenericMesh^>^ processedMeshes );
		void ProcessMeshType7( Model^ model, Node^ node, MeshType7^ mesh, List<GenericMesh^>^ processedMeshes );
		void ProcessMeshType8( Model^ model, Node^ node, MeshType8^ mesh, List<GenericMesh^>^ processedMeshes );

		FbxVector4* ConvertPositionsToFbxControlPoints( FbxVector4* fControlPoints, array<Vector3>^ positions );
		void ConvertNormalsToFbxLayerElementNormalDirectArray( FbxLayerElementNormal* fElementNormal, array<Vector3>^ normals, int vertexStart );
		void ConvertTexCoordsToFbxLayerElementUV( FbxMesh* fMesh, const char* name, array<Vector2>^ texCoords, int layer );
		void ConvertTexCoordsToFbxLayerElementUVDirectArray( FbxLayerElementUV* fElementUV, array<Vector2>^ texCoords, int vertexStart );
		void ConvertColorsToFbxLayerElementVertexColorsDirectArray( FbxLayerElementVertexColor* fElementColors, array<Color>^ colors, int vertexStart );
		void ConvertTrianglesToFbxPolygons( FbxMesh* fMesh, array<Triangle>^ triangles, int vertexStart, int materialIndex );
		FbxAMatrix ConvertNumericsMatrix4x4ToFbxAMatrix( Matrix4x4& m );
		void ConvertNodeWeightsToFbxClusters( array<array<DDS3ModelLibrary::Models::NodeWeight>^>^ weights, System::Collections::Generic::Dictionary<int, 
			System::IntPtr>^ fClusterLookup, fbxsdk::FbxScene* fScene, FbxNode* fMeshNode, fbxsdk::FbxSkin* fSkin, int vertexStart, Model^ model, GenericMesh^ mesh );

		String^ FormatNodeName( Model^ model, Node^ node );
		String^ FormatMaterialName( Model^ model, Material^ material );
		String^ FormatTextureName( TexturePack^ textures, Texture^ texture, int index );

		FbxLayer* GetFbxMeshLayer( fbxsdk::FbxMesh* fMesh, int layer );
		FbxGeometryElementNormal* CreateFbxMeshElementNormal( fbxsdk::FbxGeometryBase* fMesh );
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
		String^ mOutDir;
	};
}
