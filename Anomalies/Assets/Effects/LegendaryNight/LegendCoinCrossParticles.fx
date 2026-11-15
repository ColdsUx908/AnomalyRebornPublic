sampler uImage0 : register(s0);
float uIntensity;

float4 Main(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float4 particle = tex2D(uImage0, uv) * color;
    return float4(saturate(particle.rgb * uIntensity), particle.a);
}

technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 Main();
    }
}
