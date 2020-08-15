#include "pch.h"

#include "FbxModelExporter.h"
#include "Utf8String.h"

/* 
	FBX MODEL EXPORTER NOTES:
	- 3DS Max is very picky about the ordering of layers & layer elements in meshes.
	It assumes the following order:
	Layer 0: (maps to Map Channel 1)
		- FbxGeometryElementNormal
		- FbxGeometryElementMaterial (IndexToDirect, 1 index with value 0)
		- FbxGeometryElementColor (first color channel, if used)
		- FbxGeometryElementUV (first UV channel, if used)
	Layer 1: (maps to Map Channel 2)
		- FbxGeometryElementUV (second UV channel, if used)
	LAYER 2: (Maps to Map Channel 3)
		- FbxGeometryElementUV (second COLOR (!!!!) channel, if used)

	- EvaluateGlobalTransform() is broken. DONT USE IT
	It occasionally returns the wrong matrix, this is a known issue (just google it)
*/

// TODO: 
// - cache converted node world transform
// - add option to merge all meshes into 1
// - export morphs
// - export animations

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Numerics;

namespace DDS3ModelLibrary::Models::Conversion
{
	using namespace Textures;
	using namespace Materials;

	const double RAD_TO_DEG = 180.0 / Math::PI;

	FbxModelExporter::FbxModelExporter()
	{
		// Create manager
		mManager = FbxManager::Create();
		if ( !mManager )
			gcnew Exception( "Failed to create FBX Manager" );

		mNodeToFbxNodeLookup = gcnew Dictionary<Node^, IntPtr>();
		mConvertedNodes = gcnew List<IntPtr>();
		mMaterialCache = gcnew Dictionary<int, IntPtr>();
		mTextureCache = gcnew Dictionary<int, IntPtr>();
	}

	FbxModelExporter::~FbxModelExporter()
	{
		// Destroy manager
		mManager->Destroy();
	}

	void FbxModelExporter::Reset()
	{
		mNodeToFbxNodeLookup->Clear();
		mConvertedNodes->Clear();
		mMaterialCache->Clear();
		mTextureCache->Clear();
	}

	void FbxModelExporter::Export( Model^ model, String^ path, FbxModelExporterConfig^ config, TexturePack^ textures )
	{
		Reset();

		mConfig = config;

		// Create IO settings
		auto fIos = FbxIOSettings::Create( mManager, IOSROOT );
		fIos->SetBoolProp( EXP_FBX_MATERIAL, true );
		fIos->SetBoolProp( EXP_FBX_TEXTURE, true );
		fIos->SetBoolProp( EXP_FBX_EMBEDDED, false );
		fIos->SetBoolProp( EXP_FBX_SHAPE, true );
		fIos->SetBoolProp( EXP_FBX_GOBO, true );
		fIos->SetBoolProp( EXP_FBX_ANIMATION, true );
		fIos->SetBoolProp( EXP_FBX_GLOBAL_SETTINGS, true );
		mManager->SetIOSettings( fIos );

		// Create scene for model
		auto fScene = ConvertModelToFbxScene( model, textures );

		// Export the scene to the file
		ExportFbxScene( fScene, path );
	}

	FbxScene* FbxModelExporter::ConvertModelToFbxScene( Model^ model, TexturePack^ textures )
	{
		// Create FBX scene
		auto fScene = FbxScene::Create( mManager, "" );
		if ( !fScene )
			gcnew Exception( "Failed to create FBX scene" );

		auto& fGlobalSettings = fScene->GetGlobalSettings();
		fGlobalSettings.SetAxisSystem( FbxAxisSystem::DirectX );
		fGlobalSettings.SetSystemUnit( FbxSystemUnit::m );

		// Convert materials
		for ( size_t i = 0; i < model->Materials->Count; i++ )
			ConvertMaterialToFbxSurfacePhong( fScene, model, model->Materials[ i ], textures, i );

		// 3ds Max a bind pose. The name is taken from it as well.
		auto fBindPose = FbxPose::Create( fScene, "BIND_POSES" );
		fBindPose->SetIsBindPose( true );
		fScene->AddPose( fBindPose );

		// Create nodes first so all nodes are created while populating
		for ( size_t i = 0; i < model->Nodes->Count; i++ )
			ConvertNodeToFbxNode( model, model->Nodes[i], fScene, (int)i );

		// Fully populate the nodes
		for ( size_t i = 0; i < model->Nodes->Count; i++ )
			PopulateConvertedFbxNode( model, model->Nodes[ i ], fScene, (FbxNode*)mConvertedNodes[ i ].ToPointer() );

		return fScene;
	}

	void FbxModelExporter::ConvertMaterialToFbxSurfacePhong( FbxScene* fScene, Model^ model, Material^ mat, TexturePack^ textures, const size_t& i )
	{
		auto fMat = FbxSurfacePhong::Create( fScene, Utf8String( FormatMaterialName( model, mat ) ).ToCStr() );
		fMat->ShadingModel.Set( "Phong" );

		if ( mat->TextureId.HasValue )
		{
			auto textureName = FormatTextureName( textures, nullptr, mat->TextureId.Value );

			if ( textures != nullptr && textures->Count > mat->TextureId.Value )
			{
				// Export textures
				textures[ mat->TextureId.Value ]->GetBitmap( 0, 0 )->Save( textureName + ".png" );
			}

			// Create & connect file texture to material diffuse
			IntPtr fTexPtr;
			FbxFileTexture* fTex;
			if ( !mTextureCache->TryGetValue( mat->TextureId.Value, fTexPtr ) )
			{
				fTex = FbxFileTexture::Create( fScene, "Bitmaptexture" );
				fTex->SetFileName( Utf8String( textureName + ".png" ).ToCStr() );

				// 3ds Max sets these by default
				fTex->UVSet.Set( "UVChannel_1" );
				fTex->UseMaterial.Set( true );

				mTextureCache[ mat->TextureId.Value ] = (IntPtr)fTex;
			}
			else
			{
				fTex = (FbxFileTexture*)fTexPtr.ToPointer();
			}


			fMat->Diffuse.ConnectSrcObject( fTex );
		}

		mMaterialCache[ i ] = (IntPtr)fMat;
	}

	FbxNode* FbxModelExporter::ConvertNodeToFbxNode( Model^ model, Node^ node, FbxScene* fScene, int index )
	{
		auto fNode = FbxNode::Create( fScene, Utf8String( FormatNodeName( model, node ) ).ToCStr() );
		mNodeToFbxNodeLookup->Add( node, (IntPtr)fNode );

		// Setup transform
		fNode->LclRotation.Set( ConvertNumericsVector3RotationToFbxDouble3( node->Rotation ) );
		fNode->LclScaling.Set( ConvertNumericsVector3ToFbxDouble3( node->Scale ) );
		fNode->LclTranslation.Set( ConvertNumericsVector3ToFbxDouble3( node->Position ) );
		fNode->SetPreferedAngle( fNode->LclRotation.Get() );
		mConvertedNodes->Add( (IntPtr)fNode );
		return fNode;
	}

	void FbxModelExporter::PopulateConvertedFbxNode( Model^ model, Node^ node, FbxScene* fScene, FbxNode* fNode )
	{
		// Set parent
		FbxNode* fParentNode;
		if ( node->Parent != nullptr )
			fParentNode = (FbxNode*)mNodeToFbxNodeLookup[ node->Parent ].ToPointer();
		else
			fParentNode = fScene->GetRootNode();

		fParentNode->AddChild( fNode );

		// Create Skeleton node attribute for this node
		auto fSkeleton = FbxSkeleton::Create( fScene, "" );

		// 3ds Max always sets the skeleton type to limb node
		fSkeleton->SetSkeletonType( FbxSkeleton::EType::eLimbNode );
		fNode->SetNodeAttribute( fSkeleton );

		// Add to bind pose
		fScene->GetPose( 0 )->Add( fNode, fNode->EvaluateGlobalTransform() );

		if ( node->Geometry != nullptr )
		{
			if ( node->Geometry->Meshes != nullptr && node->Geometry->Meshes->Count > 0 )
				ConvertMeshListToFbxNodes( model, node, node->Geometry->Meshes, fScene, fNode );

			if ( node->Geometry->TranslucentMeshes != nullptr && node->Geometry->TranslucentMeshes->Count > 0 )
				ConvertMeshListToFbxNodes( model, node, node->Geometry->TranslucentMeshes, fScene, fNode );
		}

		if ( node->DeprecatedMeshList != nullptr && node->DeprecatedMeshList->Count > 0 )
			ConvertMeshListToFbxNodes( model, node, node->DeprecatedMeshList, fScene, fNode );

		if ( node->DeprecatedMeshList2 != nullptr && node->DeprecatedMeshList2->Count > 0 )
			ConvertMeshListToFbxNodes( model, node, node->DeprecatedMeshList2, fScene, fNode );
	}

	FbxDouble3 FbxModelExporter::ConvertNumericsVector3RotationToFbxDouble3( Vector3 rotation )
	{
		return FbxDouble3(
			rotation.X * RAD_TO_DEG,
			rotation.Y * RAD_TO_DEG,
			rotation.Z * RAD_TO_DEG
		);
	}

	FbxDouble3 FbxModelExporter::ConvertNumericsVector3ToFbxDouble3( Vector3 value )
	{
		return FbxDouble3( value.X, value.Y, value.Z );
	}

	void FbxModelExporter::ConvertMeshListToFbxNodes( Model^ model, Node^ node, MeshList^ meshList, FbxScene* fScene, FbxNode* fNode )
	{
		for ( size_t i = 0; i < meshList->Count; i++ )
		{
			auto mesh = meshList[ i ];
			auto fMeshNode = CreateFbxNodeForMesh( model, node, Utf8String( FormatNodeName( model, node ) + "_mesh" + i ).ToCStr(), fScene );
			auto fMesh = ConvertMeshToFbxMesh( model, node, mesh, fScene, fNode, fMeshNode );
			fMeshNode->SetNodeAttribute( fMesh );
		}
	}

	FbxNode* FbxModelExporter::CreateFbxNodeForMesh( Model^ model, Node^ node, const char* name, FbxScene* fScene )
	{
		auto fMeshNode = FbxNode::Create( fScene, name );
		fScene->GetRootNode()->AddChild( fMeshNode );

		// 3ds Max requires every node including nodes not used for skeletal animation to be in the bind pose, otherwise it is ignored entirely.
		fScene->GetPose( 0 )->Add( fMeshNode, fMeshNode->EvaluateGlobalTransform() );
		return fMeshNode;
	}

	FbxMesh* FbxModelExporter::ConvertMeshToFbxMesh( Model^ model, Node^ node, Mesh^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode )
	{
		switch ( mesh->Type )
		{
		case MeshType::Type1:
			return ConvertMeshType1ToFbxMesh( model, node, (MeshType1^)mesh, fScene, fNode, fMeshNode );

		case MeshType::Type2:
			return ConvertMeshType2ToFbxMesh( model, node, (MeshType2^)mesh, fScene, fNode, fMeshNode );

		case MeshType::Type4:
			return ConvertMeshType4ToFbxMesh( model, node, (MeshType4^)mesh, fScene, fNode, fMeshNode );

		case MeshType::Type5:
			return ConvertMeshType5ToFbxMesh( model, node, (MeshType5^)mesh, fScene, fNode, fMeshNode );

		case MeshType::Type7:
			return ConvertMeshType7ToFbxMesh( model, node, (MeshType7^)mesh, fScene, fNode, fMeshNode );

		case MeshType::Type8:
			return ConvertMeshType8ToFbxMesh( model, node, (MeshType8^)mesh, fScene, fNode, fMeshNode );
		default:
			break;
		}

		return nullptr;
	}

	FbxMesh* FbxModelExporter::ConvertMeshType1ToFbxMesh( Model^ model, Node^ node, MeshType1^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode )
	{
		// Convert batches (positions, normals)
		auto fMesh = FbxMesh::Create( fMeshNode, "" );
		fMesh->InitControlPoints( mesh->VertexCount );
		auto fControlPoints = fMesh->GetControlPoints();
		auto fElementNormal = CreateFbxMeshElementNormal( fMesh );
		CreateFbxElementMaterial( fMesh );
		auto fElementColors = CreateFbxMeshElementVertexColor( fMesh );
		auto fElementUV = CreateFbxMeshElementUV( fMesh, "UVChannel_1", 0 );
		auto fElementUV2 = mConfig->ExportMultipleUvLayers ? CreateFbxMeshElementUV( fMesh, "UVChannel_2", 1 ) : nullptr;

		int vertexStart = 0;
		for ( size_t i = 0; i < mesh->Batches->Count; i++ )
		{
			auto batch = mesh->Batches[ i ];
			auto transformed = batch->Transform( node->WorldTransform );
			ConvertPositionsToFbxControlPoints( &fControlPoints, transformed.Item1 );

			if ( batch->Normals != nullptr )
				ConvertNormalsToFbxLayerElementNormalDirectArray( fElementNormal, transformed.Item2, vertexStart );

			if ( batch->TexCoords != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV, batch->TexCoords, vertexStart );

			if ( mConfig->ExportMultipleUvLayers && batch->TexCoords2 != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV2, batch->TexCoords2, vertexStart );

			if ( batch->Colors != nullptr )
				ConvertColorsToFbxLayerElementVertexColorsDirectArray( fElementColors, batch->Colors, vertexStart );

			// Convert triangles
			ConvertTrianglesToFbxPolygons( fMesh, batch->Triangles, vertexStart );
			vertexStart += batch->VertexCount;
		}

		// Add skin that rigidly binds it to the parent node
		CreateFbxSkinForRigidParentNodeBinding( fScene, fNode, fMesh );

		// Add material to mesh node
		fMeshNode->AddMaterial( (FbxSurfaceMaterial*)mMaterialCache[ mesh->MaterialIndex ].ToPointer() );

		return fMesh;
	}

	FbxMesh* FbxModelExporter::ConvertMeshType2ToFbxMesh( Model^ model, Node^ node, MeshType2^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode )
	{
		// Convert batches (positions, normals)
		auto fMesh = FbxMesh::Create( fMeshNode, "" );
		fMesh->InitControlPoints( mesh->VertexCount );
		auto fControlPoints = fMesh->GetControlPoints();
		auto fElementNormal = CreateFbxMeshElementNormal( fMesh );
		CreateFbxElementMaterial( fMesh );
		auto fElementColors = CreateFbxMeshElementVertexColor( fMesh );
		auto fElementUV = CreateFbxMeshElementUV( fMesh, "UVChannel_1", 0 );
		auto fElementUV2 = mConfig->ExportMultipleUvLayers ? CreateFbxMeshElementUV( fMesh, "UVChannel_2", 1 ) : nullptr;

		// Create skin to hold the weights
		auto fSkin = FbxSkin::Create( fScene, "" );
		fMesh->AddDeformer( fSkin );

		auto fClusterLookup = gcnew Dictionary<int, IntPtr>();
		int vertexStart = 0;
		for ( size_t i = 0; i < mesh->Batches->Count; i++ )
		{
			auto batch = mesh->Batches[ i ];
			auto transformed = batch->Transform( model->Nodes );
			ConvertPositionsToFbxControlPoints( &fControlPoints, transformed.Item1 );

			if ( transformed.Item2 != nullptr )
				ConvertNormalsToFbxLayerElementNormalDirectArray( fElementNormal, transformed.Item2, vertexStart );

			if ( batch->TexCoords != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV, batch->TexCoords, vertexStart );

			if ( mConfig->ExportMultipleUvLayers && batch->TexCoords2 != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV2, batch->TexCoords2, vertexStart );

			if ( batch->Colors != nullptr )
				ConvertColorsToFbxLayerElementVertexColorsDirectArray( fElementColors, batch->Colors, vertexStart );

			// Convert triangles
			ConvertTrianglesToFbxPolygons( fMesh, batch->Triangles, vertexStart );

			// Convert weights
			ConvertNodeWeightsToFbxClusters( transformed.Item3, fClusterLookup, fScene, fMeshNode, fSkin, vertexStart, model );
			vertexStart += batch->VertexCount;
		}

		// Add material to mesh node
		fMeshNode->AddMaterial( (FbxSurfaceMaterial*)mMaterialCache[ mesh->MaterialIndex ].ToPointer() );

		return fMesh;
	}

	FbxMesh* FbxModelExporter::ConvertMeshType4ToFbxMesh( Model^ model, Node^ node, MeshType4^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode )
	{
		// Convert batches (positions, normals)
		auto fMesh = FbxMesh::Create( fMeshNode, "" );
		fMesh->InitControlPoints( mesh->VertexCount );
		auto fControlPoints = fMesh->GetControlPoints();
		auto fElementNormal = CreateFbxMeshElementNormal( fMesh );
		CreateFbxElementMaterial( fMesh );

		// Create skin to hold the weights
		auto transformed = mesh->Transform( node->WorldTransform );
		ConvertPositionsToFbxControlPoints( &fControlPoints, transformed.Item1 );

		if ( transformed.Item2 != nullptr )
			ConvertNormalsToFbxLayerElementNormalDirectArray( fElementNormal, transformed.Item2, 0 );

		// Convert triangles
		ConvertTrianglesToFbxPolygons( fMesh, mesh->Triangles, 0 );

		// Add skin that rigidly binds it to the parent node
		CreateFbxSkinForRigidParentNodeBinding( fScene, fNode, fMesh );

		// Add material to mesh node
		fMeshNode->AddMaterial( (FbxSurfaceMaterial*)mMaterialCache[ mesh->MaterialIndex ].ToPointer() );

		return fMesh;
	}

	FbxMesh* FbxModelExporter::ConvertMeshType5ToFbxMesh( Model^ model, Node^ node, MeshType5^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode )
	{
		// Convert batches (positions, normals)
		auto fMesh = FbxMesh::Create( fMeshNode, "" );
		fMesh->InitControlPoints( mesh->VertexCount );
		auto fControlPoints = fMesh->GetControlPoints();
		auto fElementNormal = CreateFbxMeshElementNormal( fMesh );
		CreateFbxElementMaterial( fMesh );
		auto fElementColor = CreateFbxMeshElementVertexColor( fMesh );
		auto fElementUV = CreateFbxMeshElementUV( fMesh, "UVChannel_1", 0 );
		auto fElementUV2 = mConfig->ExportMultipleUvLayers ? CreateFbxMeshElementUV( fMesh, "UVChannel_2", 1 ) : nullptr;

		// Create skin to hold the weights
		if ( mesh->UsedNodeCount == 0 )
		{
			// TODO morphers
			auto transformed = mesh->Transform( node->WorldTransform );

			ConvertPositionsToFbxControlPoints( &fControlPoints, transformed[ 0 ].Item1 );

			if ( transformed[ 0 ].Item2 != nullptr )
				ConvertNormalsToFbxLayerElementNormalDirectArray( fElementNormal, transformed[ 0 ].Item2, 0 );

			if ( mesh->TexCoords != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV, mesh->TexCoords, 0 );

			if ( mConfig->ExportMultipleUvLayers && mesh->TexCoords2 != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV2, mesh->TexCoords2, 0 );

			// Convert triangles
			ConvertTrianglesToFbxPolygons( fMesh, mesh->Triangles, 0 );

			// Add skin that rigidly binds it to the parent node
			CreateFbxSkinForRigidParentNodeBinding( fScene, fNode, fMesh );
		}
		else
		{
			auto transformed = mesh->Transform( model->Nodes );		
			ConvertPositionsToFbxControlPoints( &fControlPoints, transformed.Item1 );

			if ( transformed.Item2 != nullptr )
				ConvertNormalsToFbxLayerElementNormalDirectArray( fElementNormal, transformed.Item2, 0 );

			if ( mesh->TexCoords != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV, mesh->TexCoords, 0 );

			if ( mConfig->ExportMultipleUvLayers && mesh->TexCoords2 != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV2, mesh->TexCoords2, 0 );

			// Convert weights
			auto fSkin = FbxSkin::Create( fScene, "" );
			fMesh->AddDeformer( fSkin );
			auto fClusterLookup = gcnew Dictionary<int, IntPtr>();
			ConvertNodeWeightsToFbxClusters( transformed.Item3, fClusterLookup, fScene, fMeshNode, fSkin, 0, model );

			// Convert triangles
			ConvertTrianglesToFbxPolygons( fMesh, mesh->Triangles, 0 );
		}

		// Add material to mesh node
		fMeshNode->AddMaterial( (FbxSurfaceMaterial*)mMaterialCache[ mesh->MaterialIndex ].ToPointer() );

		return fMesh;
	}

	FbxMesh* FbxModelExporter::ConvertMeshType7ToFbxMesh( Model^ model, Node^ node, MeshType7^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode )
	{
		// Convert batches (positions, normals)
		auto fMesh = FbxMesh::Create( fMeshNode, "" );
		fMesh->InitControlPoints( mesh->VertexCount );
		auto fControlPoints = fMesh->GetControlPoints();
		auto fElementNormal = CreateFbxMeshElementNormal( fMesh );
		CreateFbxElementMaterial( fMesh );
		auto fElementUV = CreateFbxMeshElementUV( fMesh, "UVChannel_1", 0 );

		// Create skin to hold the weights
		auto fSkin = FbxSkin::Create( fScene, "" );
		fSkin->SetSkinningType( FbxSkin::EType::eLinear );
		fMesh->AddDeformer( fSkin );

		auto fClusterLookup = gcnew Dictionary<int, IntPtr>();
		int vertexStart = 0;
		for ( size_t i = 0; i < mesh->Batches->Count; i++ )
		{
			auto batch = mesh->Batches[ i ];
			auto transformed = batch->Transform( model->Nodes );
			ConvertPositionsToFbxControlPoints( &fControlPoints, transformed.Item1 );

			if ( transformed.Item2 != nullptr )
				ConvertNormalsToFbxLayerElementNormalDirectArray( fElementNormal, transformed.Item2, vertexStart );

			if ( batch->TexCoords != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV, batch->TexCoords, vertexStart );

			// Convert weights
			ConvertNodeWeightsToFbxClusters( transformed.Item3, fClusterLookup, fScene, fMeshNode, fSkin, vertexStart, model );
			vertexStart += batch->VertexCount;
		}

		// Convert triangles
		ConvertTrianglesToFbxPolygons( fMesh, mesh->Triangles, 0 );

		// Convert UV channel 2
		if ( mConfig->ExportMultipleUvLayers && mesh->TexCoords2 != nullptr )
			ConvertTexCoordsToFbxLayerElementUV( fMesh, "UVChannel_2", mesh->TexCoords2, 1 );

		// Add material to mesh node
		fMeshNode->AddMaterial( (FbxSurfaceMaterial*)mMaterialCache[ mesh->MaterialIndex ].ToPointer() );

		return fMesh;
	}

	FbxMesh* FbxModelExporter::ConvertMeshType8ToFbxMesh( Model^ model, Node^ node, MeshType8^ mesh, FbxScene* fScene, FbxNode* fNode, FbxNode* fMeshNode )
	{
		// Convert batches (positions, normals)
		auto fMesh = FbxMesh::Create( fMeshNode, "" );
		fMesh->InitControlPoints( mesh->VertexCount );
		auto fControlPoints = fMesh->GetControlPoints();
		auto fElementNormal = CreateFbxMeshElementNormal( fMesh );
		CreateFbxElementMaterial( fMesh );
		auto fElementUV = CreateFbxMeshElementUV( fMesh, "UVChannel_1", 0 );

		int vertexStart = 0;
		for ( size_t i = 0; i < mesh->Batches->Count; i++ )
		{
			auto batch = mesh->Batches[ i ];
			auto transformed = batch->Transform( node->WorldTransform );
			ConvertPositionsToFbxControlPoints( &fControlPoints, transformed.Item1 );

			if ( transformed.Item2 != nullptr )
				ConvertNormalsToFbxLayerElementNormalDirectArray( fElementNormal, transformed.Item2, vertexStart );

			if ( batch->TexCoords != nullptr )
				ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV, batch->TexCoords, vertexStart );

			vertexStart += batch->VertexCount;
		}

		// Add skin that rigidly binds it to the parent node
		CreateFbxSkinForRigidParentNodeBinding( fScene, fNode, fMesh );

		// Convert triangles
		ConvertTrianglesToFbxPolygons( fMesh, mesh->Triangles, 0 );

		// Convert UV channel 2
		if ( mConfig->ExportMultipleUvLayers && mesh->TexCoords2 != nullptr )
			ConvertTexCoordsToFbxLayerElementUV( fMesh, "UVChannel_2", mesh->TexCoords2, 1 );

		// Add material to mesh node
		fMeshNode->AddMaterial( (FbxSurfaceMaterial*)mMaterialCache[ mesh->MaterialIndex ].ToPointer() );

		return fMesh;
	}

	String^ FbxModelExporter::FormatNodeName( Model^ model, Node^ node )
	{
		if ( node->Name == nullptr ) return "node_" + model->Nodes->IndexOf( node ).ToString( "D2" );
		return node->Name;
	}

	String^ FbxModelExporter::FormatMaterialName( Model^ model, Material^ material )
	{
		return "material_" + model->Materials->IndexOf( material ).ToString( "D2" );
	}

	String^ FbxModelExporter::FormatTextureName( TexturePack^ textures, Texture^ texture, int index )
	{
		return "texture_" + index.ToString( "D2" );
	}

	// Conversion -> FBX functions
	void FbxModelExporter::ConvertPositionsToFbxControlPoints( FbxVector4** fControlPoints, array<Vector3>^ positions )
	{
		for ( size_t j = 0; j < positions->Length; j++ )
		{
			*( *fControlPoints )++ = FbxVector4( positions[ j ].X, positions[ j ].Y, positions[ j ].Z );
		}
	}

	void FbxModelExporter::ConvertNormalsToFbxLayerElementNormalDirectArray( FbxLayerElementNormal* fElementNormal, array<Vector3>^ normals, int vertexStart )
	{
		for ( size_t j = 0; j < normals->Length; j++ )
		{
			fElementNormal->GetDirectArray().SetAt( vertexStart + j, FbxVector4( normals[ j ].X, normals[ j ].Y, normals[ j ].Z ) );
		}
	}

	void FbxModelExporter::CreateFbxSkinForRigidParentNodeBinding( fbxsdk::FbxScene* fScene, fbxsdk::FbxNode* fNode, fbxsdk::FbxMesh* fMesh )
	{
		auto fSkin = FbxSkin::Create( fScene, "" );
		auto fCluster = FbxCluster::Create( fSkin, "" );
		fCluster->SetLink( fNode );
		fCluster->SetLinkMode( FbxCluster::ELinkMode::eTotalOne );
		fCluster->SetTransformLinkMatrix( fNode->EvaluateGlobalTransform() );
		for ( size_t i = 0; i < fMesh->GetControlPointsCount(); i++ ) fCluster->AddControlPointIndex( (int)i, 1 );
		fSkin->AddCluster( fCluster );
		fMesh->AddDeformer( fSkin );
	}

	void FbxModelExporter::ConvertTexCoordsToFbxLayerElementUV( FbxMesh* fMesh, const char* name, array<Vector2>^ texCoords, int layer )
	{
		auto fElementUV = CreateFbxMeshElementUV( fMesh, name, layer );
		ConvertTexCoordsToFbxLayerElementUVDirectArray( fElementUV, texCoords, 0 );
	}

	void FbxModelExporter::ConvertTexCoordsToFbxLayerElementUVDirectArray( FbxLayerElementUV* fElementUV, array<Vector2>^ texCoords, int vertexStart )
	{
		for ( size_t j = 0; j < texCoords->Length; j++ )
			fElementUV->GetDirectArray().SetAt( vertexStart + j, FbxVector2( texCoords[ j ].X, 1.0f - texCoords[ j ].Y ) );
	}

	void FbxModelExporter::ConvertColorsToFbxLayerElementVertexColorsDirectArray( FbxLayerElementVertexColor* fElementColors, array<Color>^ colors, int vertexStart )
	{
		for ( size_t i = 0; i < colors->Length; i++ )
		{
			fElementColors->GetDirectArray().SetAt( vertexStart + i, FbxColor( colors[ i ].R / 255.f, colors[ i ].G / 255.f, colors[ i ].B / 255.f, colors[ i ].A / 255.f ) );
		}
	}

	void FbxModelExporter::ConvertTrianglesToFbxPolygons( FbxMesh* fMesh, array<Triangle>^ triangles, int vertexStart )
	{
		for ( size_t i = 0; i < triangles->Length; i++ )
		{
			fMesh->BeginPolygon();
			fMesh->AddPolygon( vertexStart + triangles[ i ].A );
			fMesh->AddPolygon( vertexStart + triangles[ i ].B );
			fMesh->AddPolygon( vertexStart + triangles[ i ].C );
			fMesh->EndPolygon();
		}
	}

	FbxAMatrix FbxModelExporter::ConvertNumericsMatrix4x4ToFbxAMatrix( Matrix4x4& m )
	{
		typedef float Matrix4x4Data[ 4 ][ 4 ];

		FbxAMatrix fm;
		for ( size_t y = 0; y < 4; y++ )
		{
			for ( size_t x = 0; x < 4; x++ )
				fm[ y ][ x ] = ( *(Matrix4x4Data*)&m )[ y ][ x ];
		}

		return fm;
	}

	void FbxModelExporter::ConvertNodeWeightsToFbxClusters( array<array<NodeWeight>^>^ weights, Dictionary<int,
		System::IntPtr>^ fClusterLookup, fbxsdk::FbxScene* fScene, FbxNode* fMeshNode, fbxsdk::FbxSkin* fSkin,
		int vertexStart, Model^ model )
	{
		for ( size_t vIdx = 0; vIdx < weights->Length; vIdx++ )
		{
			for ( size_t wIdx = 0; wIdx < weights[ vIdx ]->Length; wIdx++ )
			{
				auto w = weights[ vIdx ][ wIdx ];
				if ( w.Weight == 0.0f ) continue;

				IntPtr fClusterPtr;
				FbxCluster* fCluster;
				if ( !fClusterLookup->TryGetValue( w.NodeIndex, fClusterPtr ) )
				{
					auto fNode = (FbxNode*)mConvertedNodes[ w.NodeIndex ].ToPointer();
					fCluster = FbxCluster::Create( fScene, "" );
					fCluster->SetLink( fNode );
					fCluster->SetLinkMode( FbxCluster::ELinkMode::eNormalize );

					// NOTE: DO NOT USE 'EvaluateGlobalTransform', IT IS BROKEN
					// AND DOES NOT ALWAYS RETURN THE CORRECT MATRIX!!!!
					auto worldTfm = model->Nodes[ w.NodeIndex ]->WorldTransform;
					fCluster->SetTransformLinkMatrix( ConvertNumericsMatrix4x4ToFbxAMatrix( worldTfm ) );
					fSkin->AddCluster( fCluster );
					fClusterLookup[ w.NodeIndex ] = (IntPtr)fCluster;
				}
				else
				{
					fCluster = (FbxCluster*)fClusterPtr.ToPointer();
				}

				fCluster->AddControlPointIndex( vertexStart + vIdx, w.Weight );
			}
		}
	}

	// Generic FBX helpers
	FbxLayer* FbxModelExporter::GetFbxMeshLayer( fbxsdk::FbxMesh* fMesh, int layer )
	{
		auto fLayer = fMesh->GetLayer( layer );
		if ( fLayer == nullptr )
		{
			while ( fMesh->CreateLayer() != layer );
			fLayer = fMesh->GetLayer( layer );
		}

		return fLayer;
	}

	FbxGeometryElementNormal* FbxModelExporter::CreateFbxMeshElementNormal( fbxsdk::FbxMesh* fMesh )
	{
		auto fElementNormal = fMesh->CreateElementNormal();
		fElementNormal->SetMappingMode( FbxLayerElement::EMappingMode::eByControlPoint );
		fElementNormal->SetReferenceMode( FbxLayerElement::EReferenceMode::eDirect );
		fElementNormal->GetDirectArray().SetCount( fMesh->GetControlPointsCount() );
		return fElementNormal;
	}

	FbxGeometryElementMaterial* FbxModelExporter::CreateFbxElementMaterial( fbxsdk::FbxMesh* fMesh )
	{
		// Technically not necessary to add, however 3ds Max adds this by default
		// so we do so as well to maintain compat
		auto fElementMaterial = fMesh->CreateElementMaterial();
		fElementMaterial->SetMappingMode( FbxLayerElement::EMappingMode::eAllSame );
		fElementMaterial->SetReferenceMode( FbxLayerElement::EReferenceMode::eIndexToDirect );
		fElementMaterial->GetIndexArray().Add( 0 );
		return fElementMaterial;
	}

	FbxGeometryElementUV* FbxModelExporter::CreateFbxMeshElementUV( fbxsdk::FbxMesh* fMesh, const char* name, int layer )
	{
		// 3ds Max requires every UV channel to be on its own layer (as these map to Map Channels)
		auto fLayer = GetFbxMeshLayer( fMesh, layer );
		auto fElementUV = (FbxGeometryElementUV*)fLayer->CreateLayerElementOfType( FbxLayerElement::EType::eUV );
		fElementUV->SetName( name );
		fElementUV->SetMappingMode( FbxLayerElement::EMappingMode::eByControlPoint );
		fElementUV->SetReferenceMode( FbxLayerElement::EReferenceMode::eDirect );
		fElementUV->GetDirectArray().SetCount( fMesh->GetControlPointsCount() );
		return fElementUV;
	}

	FbxGeometryElementVertexColor* FbxModelExporter::CreateFbxMeshElementVertexColor( fbxsdk::FbxMesh* fMesh )
	{
		auto fElementColors = fMesh->CreateElementVertexColor();
		fElementColors->SetMappingMode( FbxLayerElement::EMappingMode::eByControlPoint );
		fElementColors->SetReferenceMode( FbxLayerElement::EReferenceMode::eDirect );
		fElementColors->GetDirectArray().SetCount( fMesh->GetControlPointsCount() );
		return fElementColors;
	}

	void FbxModelExporter::ExportFbxScene( FbxScene* scene, String^ path )
	{
		// Create exporter
		auto exporter = FbxExporter::Create( mManager, "" );
		if ( !exporter->SetFileExportVersion( FBX_2014_00_COMPATIBLE ) )
			gcnew Exception( "Failed to set FBX export version" );

		// Initialize exporter
		auto fileFormat = mManager->GetIOPluginRegistry()->GetNativeWriterFormat();
		if ( !exporter->Initialize( Utf8String( path ).ToCStr(), fileFormat, mManager->GetIOSettings() ) )
		{
			auto errorMsg = gcnew String( exporter->GetStatus().GetErrorString() );
			gcnew Exception( "Failed to initialize FBX exporter: " + errorMsg );
		}

		// Export scene
		exporter->Export( scene );

		// Destroy exporter
		exporter->Destroy();
	}
}