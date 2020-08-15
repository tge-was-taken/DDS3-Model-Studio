#include "pch.h"

#include "FbxModelExporter.h"
#include "Utf8String.h"

using namespace System;
using namespace System::Collections::Generic;

namespace DDS3ModelLibrary::Models::Conversion
{
	using namespace Textures;

	FbxModelExporterConfig::FbxModelExporterConfig()
	{
	}

	FbxModelExporter::FbxModelExporter()
	{
	}


	static void ExportScene( FbxManager* manager, FbxScene* scene, String^ path )
	{
		// Create exporter
		auto exporter = FbxExporter::Create( manager, "FbxExporter" );
		if ( !exporter->SetFileExportVersion( FBX_2005_08_COMPATIBLE ) )
			gcnew Exception( "Failed to set FBX export version" );

		// Initialize exporter
		if ( !exporter->Initialize( Utf8String( path ).ToCStr(), -1, manager->GetIOSettings() ) )
		{
			auto errorMsg = gcnew String( exporter->GetStatus().GetErrorString() );
			gcnew Exception( "Failed to initialize FBX exporter: " + errorMsg );
		}

		// Export scene
		exporter->Export( scene );

		// Destroy exporter
		exporter->Destroy();
	}

	static FbxNode* CreateFbxNodeForNode( FbxScene* fScene, Node^ node, Dictionary<Node^, IntPtr>^ fNodeLookup )
	{
		auto fNode = FbxNode::Create( fScene, Utf8String( node->Name ).ToCStr() );
		fNodeLookup->Add( node, (IntPtr)fNode );

		// Set parent
		FbxNode* fParentNode;
		FbxSkeleton::EType fSkeletonType;
		if ( node->Parent != nullptr )
		{
			fParentNode = (FbxNode*)fNodeLookup[ node->Parent ].ToPointer();
			fSkeletonType = FbxSkeleton::EType::eLimbNode;
		}
		else
		{
			fParentNode = fScene->GetRootNode();
			fSkeletonType = FbxSkeleton::EType::eRoot;
		}

		fParentNode->AddChild( fNode );

		// Setup transform
		fNode->LclRotation.Set( FbxDouble3( node->Rotation.X, node->Rotation.Y, node->Rotation.Z ) );
		fNode->LclScaling.Set( FbxDouble3( node->Scale.X, node->Scale.Y, node->Scale.Z ) );
		fNode->LclTranslation.Set( FbxDouble3( node->Position.X, node->Position.Y, node->Position.Z ) );

		// Create Skeleton node attribute for this node
		auto fSkeleton = FbxSkeleton::Create( fScene, Utf8String( node->Name ).ToCStr() );
		fSkeleton->SetSkeletonType( fSkeletonType );
		fSkeleton->LimbLength = 0.1;
		fNode->SetNodeAttribute( fSkeleton );

		return fNode;
	}

	static FbxScene* CreateFbxSceneForModel( FbxManager* fManager, Model^ model )
	{
		// Create FBX scene
		auto fScene = FbxScene::Create( fManager, "FbxScene" );
		if ( !fScene )
			gcnew Exception( "Failed to create FBX scene" );

		auto fNodeLookup = gcnew Dictionary<Node^, IntPtr>();
		for each ( Node ^ node in model->Nodes )
		{
			auto fNode = CreateFbxNodeForNode( fScene, node, fNodeLookup );
		}

		return fScene;
	}

	void FbxModelExporter::Export( Model^ model, String^ path, FbxModelExporterConfig^ config, TexturePack^ textures )
	{
		// Create manager
		auto fManager = FbxManager::Create();
		if ( !fManager )
			gcnew Exception( "Failed to create FBX Manager" );

		// Create IO settings
		auto fIos = FbxIOSettings::Create( fManager, IOSROOT );
		fManager->SetIOSettings( fIos );

		// Create scene for model
		auto fScene = CreateFbxSceneForModel( fManager, model );

		// Export the scene to the file
		ExportScene( fManager, fScene, path );

		// Destroy manager
		fManager->Destroy();
	}
}