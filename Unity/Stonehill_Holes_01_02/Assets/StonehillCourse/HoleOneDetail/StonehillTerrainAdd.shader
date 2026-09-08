Shader "Hidden/Stonehill/Terrain Add"
{
 Properties
 {
  [HideInInspector] _Control("Control",2D)="red"{}
  [HideInInspector] _Splat0("Layer 0",2D)="white"{}
  [HideInInspector] _Splat1("Layer 1",2D)="white"{}
  [HideInInspector] _Splat2("Layer 2",2D)="white"{}
  [HideInInspector] _Splat3("Layer 3",2D)="white"{}
  [HideInInspector] _Normal0("Normal 0",2D)="bump"{}
  [HideInInspector] _Normal1("Normal 1",2D)="bump"{}
  [HideInInspector] _Normal2("Normal 2",2D)="bump"{}
  [HideInInspector] _Normal3("Normal 3",2D)="bump"{}
  _HoleMask("Hole 1 spline mask",2D)="black"{}
  _FineGrass("Package fine grass",2D)="white"{}
  _FineNormal("Package grass normal",2D)="bump"{}
  _HoleRect("Mask world bounds",Vector)=(300,1230,520,250)
 }
 SubShader
 {
  Tags {"Queue"="Geometry-99" "RenderType"="Opaque" "TerrainCompatible"="True"}
  ZWrite Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows vertex:SplatmapVert finalcolor:SplatmapFinalColor finalgbuffer:SplatmapFinalGBuffer decal:add
  #pragma target 3.0
  #pragma multi_compile_fog
  #pragma multi_compile __ _TERRAIN_NORMAL_MAP
  #define TERRAIN_SPLAT_ADDPASS
  #include "StonehillTerrain.cginc"
  ENDCG
 }
 Fallback Off
}
