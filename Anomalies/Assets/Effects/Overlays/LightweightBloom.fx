sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float2 uImageSize;
float2 uBlurStep;
float2 uDownsampleStep;
float uStrength;
float uThreshold;
float uSourceIntensity;

// KL Bloom.GaussianLinear: nine bilinear reads, legacy 5x5 total weight 0.9997.
float4 GaussianLinear(float2 uv)
{
    float2 texel = 1.0 / uImageSize;
    float2 axis = texel * (1.0 + 0.0219 / 0.1202);
    float2 corner = texel * (1.0 + 0.0133 / 0.0729);
    float4 value = tex2D(uImage0, uv) * 0.1621;
    value += (tex2D(uImage0, uv + float2(axis.x, 0))
            + tex2D(uImage0, uv - float2(axis.x, 0))
            + tex2D(uImage0, uv + float2(0, axis.y))
            + tex2D(uImage0, uv - float2(0, axis.y))) * 0.1202;
    value += (tex2D(uImage0, uv + corner) + tex2D(uImage0, uv - corner)
            + tex2D(uImage0, uv + float2(corner.x, -corner.y))
            + tex2D(uImage0, uv + float2(-corner.x, corner.y))) * 0.0892;
    return value;
}

float4 Main(float2 uv : TEXCOORD0, uniform float mode) : COLOR0
{
    if (mode > 7.5)
    {
        float4 scene = tex2D(uImage1, uv);
        float3 glow = saturate(tex2D(uImage0, uv).rgb * uStrength);
        float3 blended = 1.0 - (1.0 - min(scene.rgb, 1.0)) * (1.0 - glow);
        return float4(max(blended, scene.rgb), scene.a);
    }
    if (mode > 6.5)
        return GaussianLinear(uv) + tex2D(uImage1, uv) * uStrength;
    if (mode > 5.5)
        return GaussianLinear(uv) * uStrength;
    if (mode > 4.5)
    {
        float4 source = tex2D(uImage0, uv);
        source.rgb *= uSourceIntensity;
        return dot(source.rgb, float3(0.3, 0.59, 0.11)) > uThreshold ? source : float4(0, 0, 0, 0);
    }
    if (mode > 3.5)
    {
        float4 source = tex2D(uImage0, uv);
        float3 energy = source.rgb * uSourceIntensity;
        // 保留本体 HDR 光能；最终 LDR 目标逐通道饱和，亮芯才能随强度变白。
        // 光能倍率不改变几何覆盖度，零光能则不覆盖背景。
        return float4(energy, uSourceIntensity > 0.0 ? source.a : 0.0);
    }
    if (mode > 1.5)
    {
        float4 downsample = 0;
        [unroll]
        for (int y = 0; y < 4; y++)
        {
            [unroll]
            for (int x = 0; x < 4; x++)
            {
                float4 sampleColor = tex2D(uImage0, uv + (float2(x, y) - 1.5) * uDownsampleStep);
                if (mode > 2.5)
                {
                    float3 energy = sampleColor.rgb * uSourceIntensity;
                    float brightness = dot(energy, float3(0.3, 0.59, 0.11));
                    sampleColor = float4(brightness > uThreshold ? energy : float3(0, 0, 0), 0);
                }
                downsample += sampleColor;
            }
        }
        return downsample / 16.0;
    }
    if (mode > 0.5)
        return float4(saturate(tex2D(uImage0, uv).rgb * uStrength), 0.0);
    float4 sum = 0;
    float weightSum = 0;
    [unroll]
    for (int i = -6; i <= 6; i++)
    {
        float weight = exp(-float(i * i) / 8.0);
        sum += tex2D(uImage0, uv + uBlurStep * i) * weight;
        weightSum += weight;
    }
    return sum / weightSum;
}

technique Technique1
{
    pass Extract
    {
        PixelShader = compile ps_3_0 Main(5.0);
    }
    pass PyramidDown
    {
        PixelShader = compile ps_3_0 Main(6.0);
    }
    pass PyramidUp
    {
        PixelShader = compile ps_3_0 Main(7.0);
    }
    pass PyramidComposite
    {
        PixelShader = compile ps_3_0 Main(8.0);
    }
    pass Pass0
    {
        PixelShader = compile ps_3_0 Main(0.0);
    }
    pass Halo
    {
        PixelShader = compile ps_3_0 Main(1.0);
    }
    pass Downsample
    {
        PixelShader = compile ps_3_0 Main(2.0);
    }
    pass ExtractDownsample
    {
        PixelShader = compile ps_3_0 Main(3.0);
    }
    pass Body
    {
        PixelShader = compile ps_3_0 Main(4.0);
    }
}
