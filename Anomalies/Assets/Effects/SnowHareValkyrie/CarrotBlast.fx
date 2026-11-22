sampler uImage0 : register(s0);
float uProgress;

float4 Main(float2 uv : TEXCOORD0, float4 tint : COLOR0) : COLOR0
{
    float2 p = (uv * float2(1, 2) - 0.5) * 2;
    float radius = length(p);
    float twist = sin(radius * 9 - uProgress * 8) * 0.18 * saturate(1 - radius);
    float sine, cosine;
    sincos(twist, sine, cosine);
    float2 q = float2(cosine * p.x - sine * p.y, sine * p.x + cosine * p.y);
    float2 sampleUv = (q * 0.5 + 0.5) * float2(1, 0.5);
    return tex2D(uImage0, sampleUv) * tint;
}

technique Technique1
{
    pass Pass0 { PixelShader = compile ps_3_0 Main(); }
}
