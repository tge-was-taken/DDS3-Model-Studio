#pragma once

using namespace System;

namespace DDS3ModelLibrary::Models::Conversion
{
	public ref class FbxModelExporterConfig
	{
	public:
		FbxModelExporterConfig();
	};

	public ref class FbxModelExporter sealed : public ModelExporter<FbxModelExporter^, FbxModelExporterConfig^>
	{
	public:
		FbxModelExporter();
		void Export( Model^, String^, FbxModelExporterConfig^, DDS3ModelLibrary::Textures::TexturePack^ ) override;
	};
}
