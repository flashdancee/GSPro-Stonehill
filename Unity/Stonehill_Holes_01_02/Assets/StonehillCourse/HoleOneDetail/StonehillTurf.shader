Shader "Stonehill/Fine Turf"
{
 Properties
 {
  _MainTex ("Package turf texture", 2D) = "white" {}
  _Color ("Turf colour", Color) = (0.25,0.39,0.12,1)
  _Tile ("Texture metres", Float) = 1.5
  _Stripe ("Mowing contrast", Range(0,0.15)) = 0.045
 }
 SubShader
 {
  Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
  Offset -1,-1
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex;
  fixed4 _Color;
  float _Tile, _Stripe;
  struct Input { float3 worldPos; };
  void surf(Input IN, inout SurfaceOutputStandard o)
  {
   float2 p = IN.worldPos.xz;
   fixed3 fine = tex2D(_MainTex, p / _Tile).rgb;
   fixed3 broad = tex2D(_MainTex, p / 37.0).rgb;
   float grain = dot(fine, float3(0.25,0.60,0.15));
   float macro = dot(broad, float3(0.25,0.60,0.15));
   float stripe = smoothstep(-0.15,0.15,sin((p.x*0.82+p.y*0.57)*0.5236));
   o.Albedo = _Color.rgb * (0.85 + grain*0.30 + macro*0.12) * (1.0 + (stripe-0.5)*_Stripe*2.0);
   o.Smoothness = 0.08;
   o.Metallic = 0;
   o.Occlusion = 1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
