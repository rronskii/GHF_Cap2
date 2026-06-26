Shader "Custom/SilhouetteOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0.0, 0.1)) = 0.015
    }
    SubShader
    {
        // Renders just after standard opaque objects
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

        Pass
        {
            Name "OUTLINE"
            
            // This is the magic! It renders the inside of the mesh
            Cull Front 
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float _OutlineThickness;
            float4 _OutlineColor;

            v2f vert(appdata v)
            {
                v2f o;
                
                // Pushes the vertices outward along their normals
                float3 normal = normalize(v.normal);
                v.vertex.xyz += normal * _OutlineThickness;
                
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Paints the puffed-up inside-out mesh a solid color
                return _OutlineColor;
            }
            ENDCG
        }
    }
}