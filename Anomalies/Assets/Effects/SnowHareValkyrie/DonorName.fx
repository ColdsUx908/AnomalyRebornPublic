sampler uImage0 : register(s0);
float2 uOrigin;
float2 uScale;
float uRotation;
float uWidth;
float uTime;

float4 Main(float4 screenPos : VPOS, float2 uv : TEXCOORD0, float4 tint : COLOR0) : COLOR0
{
    float2 delta = screenPos.xy - uOrigin;
    float2 axis = float2(cos(uRotation), sin(uRotation));
    float2 p = float2(dot(delta, axis), dot(delta, float2(-axis.y, axis.x))) / max(uScale, 0.01);
    // Sky blue, cyan and pale highlights sampled from Polterghast's sprite.
    float3 color = lerp(float3(35, 200, 254), float3(140, 238, 255), saturate(p.x / max(uWidth, 1.0)));
    float ripple = sin(p.y * 0.18 + p.x * 0.08 + sin(p.x * 0.045 - uTime * 0.65) * 1.2);
    color = lerp(color, float3(0, 140, 244), (0.5 - 0.5 * ripple) * 0.19);
    color = lerp(color, float3(223, 255, 249), (0.5 + 0.5 * ripple) * 0.27);
    return float4(color / 255.0, 1.0) * tex2D(uImage0, uv).a * tint;
}

technique Technique1
{
    pass Pass0 { PixelShader = compile ps_3_0 Main(); }
}
