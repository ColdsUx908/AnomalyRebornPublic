sampler uImage0 : register(s0);
float2 uDirection;
float2 uViewportSize;
float uIntensity;
float uProgress;
float uRadius;
float uWave;
float uZoomScale;

float4 Main(float2 uv : TEXCOORD0) : COLOR0
{
    float2 delta = uv - uDirection;
    if (uWave > 0.5)
    {
        float2 p = delta * uViewportSize;
        float distancePx = length(p);
        float2 normal = p / max(distancePx, 1.0);
        float width = min(uRadius * 0.48, (52.0 + uProgress * 65.0) * uZoomScale);
        float inner = max(0.0, uRadius - width);
        // 红云的径向平方相位，只采样随前沿移动的有限环带。
        float phase = (dot(p,p) - uRadius * uRadius) / max(1.0, uRadius * uRadius - inner * inner);
        if (phase >= 0.0 || phase <= -1.0 || uIntensity <= 0.0)
            return tex2D(uImage0, uv);
        float envelope = sin(-phase * 3.14159);
        // Opposing lobes bend scene edges in both directions as the front passes.
        float ripple = envelope * sin(-phase * 6.2831853);
        float displacementPx = min(40.0 * uZoomScale, width * 0.65);
        float2 offset = normal * ripple * (displacementPx * uIntensity) / uViewportSize;
        return tex2D(uImage0, saturate(uv + offset));
    }
    float r = length(delta * uViewportSize) / max(uRadius, 1.0);
    float envelope = saturate(1.0 - r);
    float crest = 0.5 + 0.5 * cos(17.0 * r + 12.0 * uProgress);
    float2 shift = delta * (uIntensity * envelope * envelope * (0.25 + crest));
    float4 color = tex2D(uImage0, saturate(uv + shift));
    color += tex2D(uImage0, saturate(uv + shift * 0.78));
    color += tex2D(uImage0, saturate(uv + shift * 0.56));
    return color / 3.0;
}

technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 Main();
    }
}
