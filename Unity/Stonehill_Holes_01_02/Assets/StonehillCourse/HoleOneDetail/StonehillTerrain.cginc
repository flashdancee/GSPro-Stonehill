#include "UnityPBSLighting.cginc"
#define TERRAIN_STANDARD_SHADER
#define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
#include "TerrainSplatmapCommon.cginc"
sampler2D _HoleMask, _FineGrass, _FineNormal;
float4 _HoleRect;
half _Metallic0,_Metallic1,_Metallic2,_Metallic3;
half _Smoothness0,_Smoothness1,_Smoothness2,_Smoothness3;
void surf(Input IN,inout SurfaceOutputStandard o)
{
 half4 control; half weight; fixed4 diffuse;
 SplatmapMix(IN,half4(_Smoothness0,_Smoothness1,_Smoothness2,_Smoothness3),control,weight,diffuse,o.Normal);
 o.Albedo=diffuse.rgb;
 o.Smoothness=diffuse.a;
 o.Metallic=dot(control,half4(_Metallic0,_Metallic1,_Metallic2,_Metallic3));
 float2 p=IN.tc_Control*2048.0;
 float2 uv=(p-_HoleRect.xy)/_HoleRect.zw;
 fixed4 mask=tex2D(_HoleMask,uv);
 mask*=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
 float cover=saturate(mask.r+mask.g+mask.b);
 float3 grass=tex2D(_FineGrass,p/0.65).rgb;
 float grain=dot(grass,float3(0.25,0.60,0.15));
 float macro=tex2D(_FineGrass,p/41.0).g;
 float stripe=smoothstep(-0.24,0.24,sin((p.x*0.82+p.y*0.57)*0.5236));
 float3 tint=lerp(float3(0.22,0.34,0.12),float3(0.32,0.44,0.19),mask.g);
 float3 turf=tint*(0.67+grain*0.85+macro*0.16)*(1+(stripe-0.5)*lerp(0.12,0.025,mask.g));
 o.Albedo=lerp(o.Albedo,turf,cover);
 float3 fineNormal=UnpackNormal(tex2D(_FineNormal,p/0.45));
 fineNormal.xy*=0.13; fineNormal.z=sqrt(1-saturate(dot(fineNormal.xy,fineNormal.xy)));
 o.Normal=normalize(lerp(o.Normal,fineNormal,cover));
 o.Smoothness=lerp(o.Smoothness,0.08,cover);
 o.Alpha=weight;
}
