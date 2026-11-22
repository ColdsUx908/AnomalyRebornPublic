sampler uImage0 : register(s0);
float2 uOrigin;
float2 uScale;
float uTime;

float4 Main(float4 screenPos : VPOS, float2 uv : TEXCOORD0, float4 tint : COLOR0) : COLOR0
{
    float2 p = (screenPos.xy - uOrigin) / max(uScale, 0.01);
    float snow = 0.5 + 0.5 * cos(p.x * 0.075 + p.y * 0.13 - uTime * 1.5);
    float3 color = lerp(float3(0.38, 0.75, 1.0), float3(0.98, 0.99, 1.0), snow);
    // Paired rose-pink strokes lean apart like the lining of a hare's ears.
    float2 cell = float2(frac((p.x - 4.0 * uTime) / 32.0) * 32.0 - 16.0, p.y - 9.0);
    float2 ear = float2((abs(cell.x) - 2.8 + cell.y * 0.19) / 1.9, cell.y / 6.0);
    float blush = 1.0 - smoothstep(0.42, 1.2, length(ear));
    color = lerp(color, float3(1.0, 0.67, 0.82), blush * 0.85);
    return float4(color, 1.0) * tex2D(uImage0, uv).a * tint;
}

technique Technique1
{
    pass Pass0 { PixelShader = compile ps_3_0 Main(); }
}
