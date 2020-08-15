#pragma once
#include "pch.h"
#include <cassert>

using namespace System;
using namespace System::Text;

struct FbxString
{
	u8 buffer[ 1024 ];

	const char* ToCStr() const
	{
		return (const char*)buffer;
	}

	FbxString( String^ string )
	{
		const pin_ptr<const wchar_t> data = PtrToStringChars( string );
		const int length = Encoding::UTF8->GetBytes( (wchar_t*)data, string->Length, buffer, sizeof( buffer ) );
		assert( length < 1024 );
		buffer[ length ] = NULL;
	}
};