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
// - export animations
// - fix vertex colors in max
// - add unique names to root bones to prevent name clashes?

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
		mOutDir = System::IO::Path::GetDirectoryName( path );

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

		if ( textures )
		{
			// Export textures
			for ( int i = 0; i < textures->Count; i++ )
			{
				auto textureName = FormatTextureName( textures, nullptr, i );
				textures[ i ]->GetBitmap( 0, 0 )->Save(
					System::IO::Path::Combine( mOutDir, textureName + ".png" ) );
			}
		}

		// Convert materials
		for ( int i = 0; i < model->Materials->Count; i++ )
			ConvertMaterialToFbxSurfacePhong( fScene, model, model->Materials[ i ], textures, i );

		// 3ds Max a bind pose. The name is taken from it as well.
		auto fBindPose = FbxPose::Create( fScene, "BIND_POSES" );
		fBindPose->SetIsBindPose( true );
		fScene->AddPose( fBindPose );

		// Create nodes first so all nodes are created while populating
		BuildNodeToFbxNodeMapping( model, fScene );

		// Fully populate the nodes & process the meshes
		auto meshes = gcnew List<GenericMesh^>( 256 );
		for ( int i = 0; i < model->Nodes->Count; i++ )
		{
			ConvertNodeToFbxNode( model, model->Nodes[ i ], fScene, (FbxNode*)mConvertedNodes[ i ].ToPointer(),
				meshes );
		}

		if ( mConfig->MergeMeshes )
			MergeMeshes( meshes, model );

		if ( mConfig->ConvertBlendShapesToMeshes )
			ConvertBlendShapesToMeshes( meshes, model );

		// Build FBX meshes for all of the processed meshes
		for ( int i = 0; i < meshes->Count; i++ )
		{
			auto mesh = meshes[ i ];
			int faceCount = 0;
			for ( int j = 0; j < mesh->Groups->Length; j++ )
				faceCount += mesh->Groups[ j ].Triangles->Length;

			auto fMeshNode = CreateFbxNodeForMesh( fScene, Utf8String( String::Format( "mesh_{0}", i ) ).ToCStr() );
			auto work = gcnew MeshConversionContext();
			work->Mesh = FbxMesh::Create( fMeshNode, "" );
			fMeshNode->SetNodeAttribute( work->Mesh );
			work->Mesh->InitControlPoints( mesh->Vertices->Length );
			work->ControlPoints = work->Mesh->GetControlPoints();
			work->ElementNormal = mesh->Normals ? CreateFbxMeshElementNormal( work->Mesh ) : nullptr;
			work->ElementMaterial = CreateFbxElementMaterial( work->Mesh );
			work->ElementColor = mesh->Colors ? CreateFbxMeshElementVertexColor( work->Mesh ) : nullptr;
			work->ElementUV = mesh->UV1 ? CreateFbxMeshElementUV( work->Mesh, "UVChannel_1", 0 ) : nullptr;
			work->ElementUV2 = mesh->UV2 ? CreateFbxMeshElementUV( work->Mesh, "UVChannel_2", 1 ) : nullptr;

			work->Skin = FbxSkin::Create( work->Mesh, "" );
			work->Skin->SetSkinningType( FbxSkin::EType::eLinear );
			work->Mesh->AddDeformer( work->Skin );
			work->ClusterLookup = gcnew Dictionary<int, IntPtr>();

			if ( mesh->BlendShapes )
			{
				work->BlendShape = FbxBlendShape::Create( work->Mesh, "" );
				work->BlendShape->SetGeometry( work->Mesh );
				work->Mesh->AddDeformer( work->BlendShape );
			}
			
			ConvertProcessedMeshToFbxMesh( model, mesh, work, 0 );
		}

		return fScene;
	}

	void FbxModelExporter::ConvertBlendShapesToMeshes( List<GenericMesh^>^ meshes, Model^ model )
	{
		auto newMeshes = gcnew List<GenericMesh^>();

		for ( int i = 0; i < meshes->Count; i++ )
		{
			if ( !meshes[ i ]->BlendShapes ) continue;
			auto mesh = meshes[ i ];

			for ( int j = 0; j < mesh->BlendShapes->Length; j++ )
			{
				auto bs = mesh->BlendShapes[ j ];
				auto bsMesh = gcnew GenericMesh();
				bsMesh->ParentNode = mesh->ParentNode;
				bsMesh->Vertices = bs.Vertices;
				bsMesh->Normals = bs.Normals;
				bsMesh->Colors = mesh->Colors;
				bsMesh->UV1 = mesh->UV1;
				bsMesh->UV2 = mesh->UV2;
				bsMesh->Weights = mesh->Weights;
				bsMesh->Groups = mesh->Groups;
				newMeshes->Add( bsMesh );
			}

			mesh->BlendShapes = nullptr;
		}

		for ( int i = 0; i < newMeshes->Count; i++ )
			meshes->Add( newMeshes[ i ] );
	}

	void FbxModelExporter::MergeMeshes( List<GenericMesh^>^& meshes, Model^ model )
	{
		auto newMeshes = gcnew List<GenericMesh^>( 2 );

		// calculate vertex count
		int vertexCount = 0;
		int groupCount = 0;
		bool usesNormals = false, usesColors = false, usesUV1 = false, 
			usesUV2 = false, usesWeights = false;
		for ( int i = 0; i < meshes->Count; i++ )
		{
			if ( meshes[ i ]->BlendShapes ) continue;
			vertexCount += meshes[ i ]->Vertices->Length;
			groupCount += meshes[ i ]->Groups->Length;
			if ( meshes[ i ]->Normals ) usesNormals = true;
			if ( meshes[ i ]->Colors ) usesColors = true;
			if ( meshes[ i ]->UV1 ) usesUV1 = true;
			if ( meshes[ i ]->UV2 ) usesUV2 = true;
			if ( meshes[ i ]->Weights ) usesWeights = true;
		}

		// build merged mesh
		auto mergedMesh = gcnew GenericMesh();
		mergedMesh->ParentNode = model->Nodes[ 0 ];
		mergedMesh->Vertices = gcnew array<Vector3>( vertexCount );
		mergedMesh->Normals = usesNormals ? gcnew array<Vector3>( vertexCount ) : nullptr;
		mergedMesh->Colors = usesColors ? gcnew array<Color>( vertexCount ) : nullptr;
		mergedMesh->UV1 = usesUV1 ? gcnew array<Vector2>( vertexCount ) : nullptr;
		mergedMesh->UV2 = usesUV2 ? gcnew array<Vector2>( vertexCount ) : nullptr;
		mergedMesh->Weights = usesWeights ? gcnew array<array<NodeWeight>^>( vertexCount ) : nullptr;
		mergedMesh->Groups = gcnew array<GenericPrimitiveGroup>( groupCount );
		newMeshes->Add( mergedMesh );

		// merge meshes
		int vertexOffset = 0;
		int groupOffset = 0;
		for ( int i = 0; i < meshes->Count; i++ )
		{
			if ( meshes[ i ]->BlendShapes ) continue;
			auto mesh = meshes[ i ];

			Array::Copy( mesh->Vertices, 0, mergedMesh->Vertices, vertexOffset, mesh->Vertices->Length );
			if ( mesh->Normals ) Array::Copy( mesh->Normals, 0, mergedMesh->Normals, vertexOffset, mesh->Normals->Length );
			if ( mesh->Colors ) Array::Copy( mesh->Colors, 0, mergedMesh->Colors, vertexOffset, mesh->Colors->Length );
			if ( mesh->UV1 ) Array::Copy( mesh->UV1, 0, mergedMesh->UV1, vertexOffset, mesh->UV1->Length );
			if ( mesh->UV2 ) Array::Copy( mesh->UV2, 0, mergedMesh->UV2, vertexOffset, mesh->UV2->Length );

			if ( mesh->Weights ) Array::Copy( mesh->Weights, 0, mergedMesh->Weights, vertexOffset, mesh->Weights->Length );
			else if ( usesWeights )
			{
				// generate weights
				auto nodeIndex = model->Nodes->IndexOf( mesh->ParentNode );
				for ( int i = 0; i < mesh->Vertices->Length; i++ )
				{
					auto weights = mergedMesh->Weights[ vertexOffset + i ] = gcnew array<NodeWeight>( 1 );
					weights[ 0 ].NodeIndex = nodeIndex;
					weights[ 0 ].Weight = 1;
				}
			}

			Array::Copy( mesh->Groups, 0, mergedMesh->Groups, groupOffset, mesh->Groups->Length );
			for ( int j = 0; j < mesh->Groups->Length; j++ )
			{
				// adjust vertex indices
				auto% grp = mergedMesh->Groups[ groupOffset + j ];
				for ( int k = 0; k < grp.Triangles->Length; k++ )
				{
					grp.Triangles[ k ].A += vertexOffset;
					grp.Triangles[ k ].B += vertexOffset;
					grp.Triangles[ k ].C += vertexOffset;
				}
			}

			vertexOffset += mesh->Vertices->Length;
			groupOffset += mesh->Groups->Length;
		}

		// add blend shape meshes as-is
		for ( int i = 0; i < meshes->Count; i++ )
		{
			if ( !meshes[ i ]->BlendShapes ) continue;
			newMeshes->Add( meshes[ i ] );
		}

		meshes->Clear();
		meshes = newMeshes;
	}

	FbxNode* FbxModelExporter::CreateFbxNodeForMesh( FbxScene* fScene, const char* name )
	{
		auto fMeshNode = FbxNode::Create( fScene, name );
		fScene->GetRootNode()->AddChild( fMeshNode );

		// 3ds Max requires every node including nodes not used for skeletal animation to be in the bind pose, otherwise it is ignored entirely.
		fScene->GetPose( 0 )->Add( fMeshNode, fMeshNode->EvaluateGlobalTransform() );
		return fMeshNode;
	}

	void FbxModelExporter::ConvertProcessedMeshToFbxMesh( Model^ model, GenericMesh^ mesh, MeshConversionContext^ work, int vertexStart )
	{
		work->ControlPoints = ConvertPositionsToFbxControlPoints( work->ControlPoints, mesh->Vertices );

		if ( mesh->Normals ) ConvertNormalsToFbxLayerElementNormalDirectArray( work->ElementNormal, mesh->Normals, vertexStart );
		if ( mesh->Colors ) ConvertColorsToFbxLayerElementVertexColorsDirectArray( work->ElementColor, mesh->Colors, vertexStart );
		if ( mesh->UV1 ) ConvertTexCoordsToFbxLayerElementUVDirectArray( work->ElementUV, mesh->UV1, vertexStart );
		if ( mesh->UV2 ) ConvertTexCoordsToFbxLayerElementUVDirectArray( work->ElementUV2, mesh->UV2, vertexStart );

		if ( mesh->Weights )
		{
			ConvertNodeWeightsToFbxClusters( mesh->Weights, work->ClusterLookup, work->Mesh->GetScene(), work->Mesh->GetNode(), work->Skin, 
				vertexStart, model, mesh );
		}
		else
		{
			// Add cluster that rigidly binds it to the parent node
			AddRigidFbxClusterForParentNode( model, mesh, work->Skin, work->ClusterLookup, vertexStart );
		}

		if ( mesh->BlendShapes )
		{
			for ( int i = 0; i < mesh->BlendShapes->Length; i++ )
			{
				auto blendShape = mesh->BlendShapes[ i ];

				// Each shape will be stored in its own channel
				auto fChannel = FbxBlendShapeChannel::Create( work->Mesh, "" );
				work->BlendShape->AddBlendShapeChannel( fChannel );

				auto fShape = FbxShape::Create( work->Mesh, "" );
				fChannel->AddTargetShape( fShape );
				fShape->SetAbsoluteMode( true );				

				// Convert vertices
				fShape->InitControlPoints( blendShape.Vertices->Length );
				ConvertPositionsToFbxControlPoints( fShape->GetControlPoints(), blendShape.Vertices );

				// Convert normals
				fShape->InitNormals( blendShape.Normals->Length );
				FbxLayerElementArrayTemplate<FbxVector4>* fNormalsArray;
				fShape->GetNormals( &fNormalsArray );
				auto fNormals = (FbxVector4*)fNormalsArray->GetLocked();
				ConvertPositionsToFbxControlPoints( fNormals, blendShape.Normals );
			}
		}

		int faceOffset = 0;
		for ( int i = 0; i < mesh->Groups->Length; i++ )
		{
			auto grp = mesh->Groups[ i ];

			// Add material to mesh node 
			auto fMaterial = (FbxSurfaceMaterial*)mMaterialCache[ grp.MaterialIndex ].ToPointer();
			auto materialIndex = work->Mesh->GetNode()->GetMaterialIndex( fMaterial->GetNameOnly() ); 
			if ( materialIndex == -1 )
			{
				materialIndex = work->Mesh->GetNode()->AddMaterial( fMaterial );
				assert( materialIndex >= 0 );
			}

			// Convert triangles
			ConvertTrianglesToFbxPolygons( work->Mesh, grp.Triangles, vertexStart, materialIndex );
			faceOffset += grp.Triangles->Length;
		}
	}

	void FbxModelExporter::AddRigidFbxClusterForParentNode( Model^ model, GenericMesh^ mesh, 
		FbxSkin* fSkin, Dictionary<int, IntPtr>^ fClusterLookup, int vertexStart )
	{
		IntPtr fClusterPtr;
		auto nodeIndex = model->Nodes->IndexOf( mesh->ParentNode );
		if ( !fClusterLookup->TryGetValue( nodeIndex, fClusterPtr ) )
		{
			auto fCluster = FbxCluster::Create( fSkin, "" );
			fCluster->SetLink( (FbxNode*)mNodeToFbxNodeLookup[ mesh->ParentNode ].ToPointer() );
			fCluster->SetLinkMode( FbxCluster::ELinkMode::eNormalize );
			fCluster->SetTransformLinkMatrix( fCluster->GetLink()->EvaluateGlobalTransform() );
			fSkin->AddCluster( fCluster );
			fClusterPtr = (IntPtr)fCluster;
			fClusterLookup[ nodeIndex ] = fClusterPtr;
		}

		auto fCluster = (FbxCluster*)fClusterPtr.ToPointer();
		for ( int j = 0; j < mesh->Vertices->Length; j++ )
			fCluster->AddControlPointIndex( vertexStart + j, 1 );
	}

	void FbxModelExporter::ConvertMaterialToFbxSurfacePhong( FbxScene* fScene, Model^ model, Material^ mat, TexturePack^ textures, const int& i )
	{
		auto fMat = FbxSurfacePhong::Create( fScene, Utf8String( FormatMaterialName( model, mat ) ).ToCStr() );
		fMat->ShadingModel.Set( "Phong" );

		if ( mat->TextureId.HasValue )
		{
			auto textureName = FormatTextureName( textures, nullptr, mat->TextureId.Value );

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

	void FbxModelExporter::BuildNodeToFbxNodeMapping( Model^ model, FbxScene* fScene )
	{
		for ( int i = 0; i < model->Nodes->Count; i++ )
		{
			auto node = model->Nodes[ i ];
			auto fNode = FbxNode::Create( fScene, Utf8String( FormatNodeName( model, node ) ).ToCStr() );
			mNodeToFbxNodeLookup->Add( node, (IntPtr)fNode );
			mConvertedNodes->Add( (IntPtr)fNode );
		}
	}

	void FbxModelExporter::ConvertNodeToFbxNode( Model^ model, Node^ node, FbxScene* fScene, FbxNode* fNode, List<GenericMesh^>^ meshes )
	{
		// Setup transform
		fNode->LclRotation.Set( ConvertNumericsVector3RotationToFbxDouble3( node->Rotation ) );
		fNode->LclScaling.Set( ConvertNumericsVector3ToFbxDouble3( node->Scale ) );
		fNode->LclTranslation.Set( ConvertNumericsVector3ToFbxDouble3( node->Position ) );
		fNode->SetPreferedAngle( fNode->LclRotation.Get() );

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
				ProcessMeshList( model, node, node->Geometry->Meshes, meshes );

			if ( node->Geometry->TranslucentMeshes != nullptr && node->Geometry->TranslucentMeshes->Count > 0 )
				ProcessMeshList( model, node, node->Geometry->TranslucentMeshes, meshes );
		}

		if ( node->DeprecatedMeshList != nullptr && node->DeprecatedMeshList->Count > 0 )
			ProcessMeshList( model, node, node->DeprecatedMeshList, meshes );

		if ( node->DeprecatedMeshList2 != nullptr && node->DeprecatedMeshList2->Count > 0 )
			ProcessMeshList( model, node, node->DeprecatedMeshList2, meshes );
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

	void FbxModelExporter::ProcessMeshList( Model^ model, Node^ node, MeshList^ meshList, List<GenericMesh^>^ processedMeshes )
	{
		for ( int i = 0; i < meshList->Count; i++ )
			ProcessMesh( model, node, meshList[i], processedMeshes );
	}

	FbxNode* FbxModelExporter::CreateFbxNodeForMesh( Model^ model, Node^ node, const char* name, FbxScene* fScene )
	{
		auto fMeshNode = FbxNode::Create( fScene, name );
		fScene->GetRootNode()->AddChild( fMeshNode );

		// 3ds Max requires every node including nodes not used for skeletal animation to be in the bind pose, otherwise it is ignored entirely.
		fScene->GetPose( 0 )->Add( fMeshNode, fMeshNode->EvaluateGlobalTransform() );
		return fMeshNode;
	}

	void FbxModelExporter::ProcessMesh( Model^ model, Node^ node, Mesh^ mesh, List<GenericMesh^>^ meshes )
	{
		switch ( mesh->Type )
		{
		case MeshType::Type1:
			return ProcessMeshType1( model, node, (MeshType1^)mesh, meshes );

		case MeshType::Type2:
			return ProcessMeshType2( model, node, (MeshType2^)mesh, meshes );

		case MeshType::Type4:												 
			return ProcessMeshType4( model, node, (MeshType4^)mesh, meshes );
																			 
		case MeshType::Type5:												
			return ProcessMeshType5( model, node, (MeshType5^)mesh, meshes );
																			
		case MeshType::Type7:												 
			return ProcessMeshType7( model, node, (MeshType7^)mesh, meshes );
																			
		case MeshType::Type8:												
			return ProcessMeshType8( model, node, (MeshType8^)mesh, meshes );
		default:
			break;
		}
	}

	void FbxModelExporter::ProcessMeshType1( Model^ model, Node^ node, MeshType1^ mesh, List<GenericMesh^>^ meshes )
	{
		for ( int i = 0; i < mesh->Batches->Count; i++ )
		{
			auto batch = mesh->Batches[ i ];
			auto transformed = batch->Transform( node->WorldTransform );
			auto gMesh = gcnew GenericMesh();
			gMesh->Source = mesh;
			gMesh->ParentNode = node;
			gMesh->Vertices = transformed.Item1;
			gMesh->Normals = transformed.Item2;
			gMesh->UV1 = batch->TexCoords;
			gMesh->UV2 = batch->TexCoords2;
			gMesh->Colors = batch->Colors;
			gMesh->Groups = gcnew array<GenericPrimitiveGroup>( 1 );
			gMesh->Groups[ 0 ].MaterialIndex = mesh->MaterialIndex;
			gMesh->Groups[ 0 ].Triangles = batch->Triangles;
			meshes->Add( gMesh );
		}
	}

	void FbxModelExporter::ProcessMeshType2( Model^ model, Node^ node, MeshType2^ mesh, List<GenericMesh^>^ meshes )
	{
		for ( int i = 0; i < mesh->Batches->Count; i++ )
		{
			auto batch = mesh->Batches[ i ];
			auto transformed = batch->Transform( model->Nodes );
			auto gMesh = gcnew GenericMesh();
			gMesh->Source = mesh;
			gMesh->ParentNode = node;
			gMesh->Vertices = transformed.Item1;
			gMesh->Normals = transformed.Item2;
			gMesh->UV1 = batch->TexCoords;
			gMesh->UV2 = batch->TexCoords2;
			gMesh->Colors = batch->Colors;
			gMesh->Weights = transformed.Item3;
			gMesh->Groups = gcnew array<GenericPrimitiveGroup>( 1 );
			gMesh->Groups[ 0 ].MaterialIndex = mesh->MaterialIndex;
			gMesh->Groups[ 0 ].Triangles = batch->Triangles;
			meshes->Add( gMesh );
		}
	}

	void FbxModelExporter::ProcessMeshType4( Model^ model, Node^ node, MeshType4^ mesh, List<GenericMesh^>^ meshes )
	{
		auto transformed = mesh->Transform( node->WorldTransform );
		auto gMesh = gcnew GenericMesh();
		gMesh->Source = mesh;
		gMesh->ParentNode = node;
		gMesh->Vertices = transformed.Item1;
		gMesh->Normals = transformed.Item2;
		gMesh->Groups = gcnew array<GenericPrimitiveGroup>( 1 );
		gMesh->Groups[ 0 ].MaterialIndex = mesh->MaterialIndex;
		gMesh->Groups[ 0 ].Triangles = mesh->Triangles;
		meshes->Add( gMesh );
	}

	void FbxModelExporter::ProcessMeshType5( Model^ model, Node^ node, MeshType5^ mesh, List<GenericMesh^>^ meshes )
	{
		if ( mesh->UsedNodeCount == 0 )
		{
			auto transformed = mesh->Transform( node->WorldTransform );
			auto gMesh = gcnew GenericMesh();
			gMesh->Source = mesh;
			gMesh->ParentNode = node;
			gMesh->Vertices = transformed[0].Item1;
			gMesh->Normals = transformed[0].Item2;
			gMesh->UV1 = mesh->TexCoords;
			gMesh->UV2 = mesh->TexCoords2;
			gMesh->Groups = gcnew array<GenericPrimitiveGroup>( 1 );
			gMesh->Groups[ 0 ].MaterialIndex = mesh->MaterialIndex;
			gMesh->Groups[ 0 ].Triangles = mesh->Triangles;

			gMesh->BlendShapes = gcnew array<GenericBlendShape>( transformed->Length - 1 );
			for ( int i = 0; i < gMesh->BlendShapes->Length; i++ )
			{
				auto morphData = GenericBlendShape();
				morphData.Vertices = transformed[ 1 + i ].Item1;
				morphData.Normals = transformed[ 1 + i ].Item2;
				gMesh->BlendShapes[ i ] = morphData;
			}

			meshes->Add( gMesh );
		}
		else
		{
			auto transformed = mesh->Transform( model->Nodes );		
			auto gMesh = gcnew GenericMesh();
			gMesh->Source = mesh;
			gMesh->ParentNode = node;
			gMesh->Vertices = transformed.Item1;
			gMesh->Normals = transformed.Item2;
			gMesh->UV1 = mesh->TexCoords;
			gMesh->UV2 = mesh->TexCoords2;
			gMesh->Weights = transformed.Item3;
			gMesh->Groups = gcnew array<GenericPrimitiveGroup>( 1 );
			gMesh->Groups[ 0 ].MaterialIndex = mesh->MaterialIndex;
			gMesh->Groups[ 0 ].Triangles = mesh->Triangles;
			meshes->Add( gMesh );
		}
	}

	void FbxModelExporter::ProcessMeshType7( Model^ model, Node^ node, MeshType7^ mesh, List<GenericMesh^>^ meshes )
	{
		auto gMesh = gcnew GenericMesh();
		gMesh->Source = mesh;
		gMesh->ParentNode = node;
		gMesh->Vertices = gcnew array<Vector3>( mesh->VertexCount );
		gMesh->UV2 = mesh->TexCoords2;
		gMesh->Weights = gcnew array<array<NodeWeight>^>( mesh->VertexCount );
		gMesh->Groups = gcnew array<GenericPrimitiveGroup>( 1 );
		gMesh->Groups[ 0 ].MaterialIndex = mesh->MaterialIndex;
		gMesh->Groups[ 0 ].Triangles = mesh->Triangles;

		int vertexStart = 0;
		for ( int i = 0; i < mesh->Batches->Count; i++ )
		{
			auto batch = mesh->Batches[ i ];
			auto transformed = batch->Transform( model->Nodes );
			Array::Copy( transformed.Item1, 0, gMesh->Vertices, vertexStart, transformed.Item1->Length );

			if ( transformed.Item2 != nullptr )
			{
				if ( gMesh->Normals == nullptr ) gMesh->Normals = gcnew array<Vector3>( mesh->VertexCount );
				Array::Copy( transformed.Item2, 0, gMesh->Normals, vertexStart, transformed.Item2->Length );
			}

			if ( batch->TexCoords != nullptr )
			{
				if ( gMesh->UV1 == nullptr ) gMesh->UV1 = gcnew array<Vector2>( mesh->VertexCount );
				Array::Copy( batch->TexCoords, 0, gMesh->UV1, vertexStart, batch->TexCoords->Length );
			}

			Array::Copy( transformed.Item3, 0, gMesh->Weights, vertexStart, transformed.Item3->Length );
			vertexStart += batch->VertexCount;
		}

		meshes->Add( gMesh );
	}

	void FbxModelExporter::ProcessMeshType8( Model^ model, Node^ node, MeshType8^ mesh, List<GenericMesh^>^ meshes )
	{
		auto gMesh = gcnew GenericMesh();
		gMesh->Source = mesh;
		gMesh->ParentNode = node;
		gMesh->Vertices = gcnew array<Vector3>( mesh->VertexCount );
		gMesh->UV2 = mesh->TexCoords2;
		gMesh->Groups = gcnew array<GenericPrimitiveGroup>( 1 );
		gMesh->Groups[ 0 ].MaterialIndex = mesh->MaterialIndex;
		gMesh->Groups[ 0 ].Triangles = mesh->Triangles;

		int vertexStart = 0;
		for ( int i = 0; i < mesh->Batches->Count; i++ )
		{
			auto batch = mesh->Batches[ i ];
			auto transformed = batch->Transform( node->WorldTransform );
			Array::Copy( transformed.Item1, 0, gMesh->Vertices, vertexStart, transformed.Item1->Length );

			if ( transformed.Item2 != nullptr )
			{
				if ( gMesh->Normals == nullptr ) gMesh->Normals = gcnew array<Vector3>( mesh->VertexCount );
				Array::Copy( transformed.Item2, 0, gMesh->Normals, vertexStart, transformed.Item2->Length );
			}

			if ( batch->TexCoords != nullptr )
			{
				if ( gMesh->UV1 == nullptr ) gMesh->UV1 = gcnew array<Vector2>( mesh->VertexCount );
				Array::Copy( batch->TexCoords, 0, gMesh->UV1, vertexStart, batch->TexCoords->Length );
			}

			vertexStart += batch->VertexCount;
		}

		meshes->Add( gMesh );
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
	FbxVector4* FbxModelExporter::ConvertPositionsToFbxControlPoints( FbxVector4* fControlPoints, array<Vector3>^ positions )
	{
		for ( int j = 0; j < positions->Length; j++ )
		{
			*fControlPoints++ = FbxVector4( positions[ j ].X, positions[ j ].Y, positions[ j ].Z );
		}

		return fControlPoints;
	}

	void FbxModelExporter::ConvertNormalsToFbxLayerElementNormalDirectArray( FbxLayerElementNormal* fElementNormal, array<Vector3>^ normals, int vertexStart )
	{
		for ( int j = 0; j < normals->Length; j++ )
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
		for ( int i = 0; i < fMesh->GetControlPointsCount(); i++ ) fCluster->AddControlPointIndex( (int)i, 1 );
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
		for ( int j = 0; j < texCoords->Length; j++ )
			fElementUV->GetDirectArray().SetAt( vertexStart + j, FbxVector2( texCoords[ j ].X, 1.0f - texCoords[ j ].Y ) );
	}

	void FbxModelExporter::ConvertColorsToFbxLayerElementVertexColorsDirectArray( FbxLayerElementVertexColor* fElementColors, array<Color>^ colors, int vertexStart )
	{
		for ( int i = 0; i < colors->Length; i++ )
		{
			fElementColors->GetDirectArray().SetAt( vertexStart + i, 
				FbxColor( (double)colors[ i ].R / 255.0, (float)colors[ i ].G / 255.0, 
						  (double)colors[ i ].B / 255.0, (float)colors[ i ].A / 128.0 ) );
		}
	}

	void FbxModelExporter::ConvertTrianglesToFbxPolygons( FbxMesh* fMesh, array<Triangle>^ triangles, int vertexStart, int materialIndex )
	{
		for ( int i = 0; i < triangles->Length; i++ )
		{
			fMesh->BeginPolygon( materialIndex, -1, -1, false );
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
		for ( int y = 0; y < 4; y++ )
		{
			for ( int x = 0; x < 4; x++ )
				fm[ y ][ x ] = ( *(Matrix4x4Data*)&m )[ y ][ x ];
		}

		return fm;
	}

	void FbxModelExporter::ConvertNodeWeightsToFbxClusters( array<array<NodeWeight>^>^ weights, Dictionary<int,
		System::IntPtr>^ fClusterLookup, fbxsdk::FbxScene* fScene, FbxNode* fMeshNode, fbxsdk::FbxSkin* fSkin,
		int vertexStart, Model^ model, GenericMesh^ mesh )
	{
		for ( int vIdx = 0; vIdx < weights->Length; vIdx++ )
		{
			auto vWeights = weights[ vIdx ];
			if ( vWeights )
			{
				for ( int wIdx = 0; wIdx < vWeights->Length; wIdx++ )
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
			else
			{
				// Add cluster that rigidly binds it to the parent node
				AddRigidFbxClusterForParentNode( model, mesh, fSkin, fClusterLookup, vertexStart );
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

	FbxGeometryElementNormal* FbxModelExporter::CreateFbxMeshElementNormal( fbxsdk::FbxGeometryBase* fMesh )
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
		fElementMaterial->SetMappingMode( FbxLayerElement::EMappingMode::eByPolygon );
		fElementMaterial->SetReferenceMode( FbxLayerElement::EReferenceMode::eIndexToDirect );
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
		for ( int i = 0; i < fElementColors->GetDirectArray().GetCount(); i++ )
			fElementColors->GetDirectArray().SetAt( i, FbxColor( 1, 1, 1, 1 ) );

		return fElementColors;
	}

	void FbxModelExporter::ExportFbxScene( FbxScene* scene, String^ path )
	{
		// Create exporter
		auto exporter = FbxExporter::Create( mManager, "" );
		if ( !exporter->SetFileExportVersion( FBX_2014_00_COMPATIBLE ) )
			gcnew Exception( "Failed to set FBX export version" );

		// Initialize exporter
		//auto fileFormat = mManager->GetIOPluginRegistry()->GetNativeWriterFormat();
		if ( !exporter->Initialize( Utf8String( path ).ToCStr(), -1, mManager->GetIOSettings() ) )
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