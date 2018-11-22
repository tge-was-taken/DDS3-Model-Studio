using System;
using System.Collections.Generic;
using System.IO;
using DDS3ModelLibrary.Data;
using Newtonsoft.Json;

namespace DDS3ModelLibrary.Materials
{
    public static class MaterialPresetStore
    {
        private static Dictionary<int, int> sMaterialHashToId;
        private static HashSet<int> sValidPresetIds;

        static MaterialPresetStore()
        {
            LoadIndex();
        }

        private static void LoadIndex()
        {
            sValidPresetIds = new HashSet<int>();
            var hashToIdJsonPath = GetPath( "index.json" );
            if ( File.Exists( hashToIdJsonPath ) )
            {
                sMaterialHashToId = JsonConvert.DeserializeObject<Dictionary<int, int>>( File.ReadAllText( hashToIdJsonPath ) );
                foreach ( int value in sMaterialHashToId.Values )
                    sValidPresetIds.Add( value );
            }
            else
            {
                sMaterialHashToId = new Dictionary<int, int>();
            }
        }

        private static string GetPath( string path ) => ResourceStore.GetPath( "material_presets\\" + path );

        public static bool IsPreset( Material material ) => sMaterialHashToId.ContainsKey( material.GetPresetHashCode() );

        public static int GetPresetId( Material material ) => sMaterialHashToId[ material.GetPresetHashCode() ];

        public static bool IsValidPresetId( int id ) => sValidPresetIds.Contains( id );

        //private static Material CreateMaterialFromPreset( int id, bool hasTexture, bool hasOverlay )
        //{
        //    var material = FromPresetInternal( id, hasTexture, hasOverlay );
        //    if ( material != null )
        //        return material;

        //    if ( hasOverlay )
        //    {
        //        // No template with overlay found, try without overlay
        //        material = FromPresetInternal( id, hasTexture, false );
        //        if ( material != null )
        //            return material;
        //    }

        //    if ( hasTexture )
        //    {
        //        // No template without overlay found, try without texture
        //        material = FromPresetInternal( id, false, false );
        //        if ( material != null )
        //            return material;
        //    }

        //    // Preset doesn't exist
        //    throw new ArgumentOutOfRangeException( nameof( id ), $"Material preset {id} does not exist" );
        //}

        public static Material GetPreset( int id, bool hasTexture, bool hasOverlay )
        {
            var path = GetMaterialPresetPath( id, hasTexture, hasOverlay );
            if ( !File.Exists( path ) )
                throw new ArgumentOutOfRangeException( nameof( id ), $"Material preset {id} does not exist" );

            var json = File.ReadAllText( path );
            return JsonConvert.DeserializeObject<Material>( json );
        }

        private static string GetMaterialPresetPath( int id, bool hasTexture, bool hasOverlay )
        {
            var name = id.ToString();
            if ( hasTexture )
                name += "_d";

            if ( hasOverlay )
                name += "_o";

            return GetPath( name + ".json" );
        }
    }
}
