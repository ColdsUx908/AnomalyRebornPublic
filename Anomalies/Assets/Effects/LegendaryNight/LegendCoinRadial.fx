float4x4 uInverseViewMatrix;
float2 uScreenPosition;
float2 uResolution;
float2 uCenter;
float uOuterRadius;
float uInnerRadius;
float uTime;
float uEnvelope;
float2 uWave;
float3 uShade;
float3 uColor;
float3 uLight;
texture uFlow;
texture uStreak;
texture uNoise;
sampler uFlowSampler : register(s1) = sampler_state { Texture = <uFlow>; MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Wrap; AddressV = Wrap; };
sampler uStreakSampler : register(s2) = sampler_state { Texture = <uStreak>; MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Wrap; AddressV = Wrap; };
sampler uNoiseSampler : register(s3) = sampler_state { Texture = <uNoise>; MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Wrap; AddressV = Wrap; };

float4 Main(float2 uv : TEXCOORD0) : COLOR0
{
    float2 world = mul(float4(uv * uResolution, 0, 1), uInverseViewMatrix).xy + uScreenPosition;
    float2 offset = world - uCenter;
    float radius = length(offset);
    float aa = max(fwidth(radius), 1.0);
    float outer = saturate((uOuterRadius - radius) / aa + 0.5);
    float inner = uInnerRadius > 0.0 ? saturate((radius - uInnerRadius) / aa + 0.5) : 1.0;
    float coverage = outer * inner;
    if (coverage <= 0.0)
        return float4(0, 0, 0, 0);
    float angle = atan2(offset.y, offset.x) / 6.2831853;
    float band = max(uOuterRadius - uInnerRadius, 1.0);
    float along = (radius - uInnerRadius) / band;
    // 钱币的同心刻线与交错金属光流，共用十字的流纹，避免复制星爆的扩张火焰。
    float noise = tex2D(uNoiseSampler, float2(angle * 4.0 + uTime * .004, radius / 390.0 - uTime * .006)).r;
    float flow = tex2D(uFlowSampler, float2(angle * 6.0 - uTime * .011, radius / 260.0 + noise * .18)).r;
    float streak = tex2D(uStreakSampler, float2(angle * 9.0 + uTime * .016, radius / 150.0 - uTime * .009)).r;
    float outerRim = exp(-pow((uOuterRadius - radius) / 19.0, 2.0));
    float innerRim = uInnerRadius > 0.0 ? exp(-pow((radius - uInnerRadius) / 19.0, 2.0)) : 0.0;
    float rim = max(outerRim, innerRim);
    float engraving = pow(saturate(.5 + .5 * cos(along * 37.6991 - uTime * .035)), 18.0);
    float spokes = pow(saturate(.5 + .5 * cos(angle * 100.53096 + uTime * .012)), 28.0);
    float pulse = exp(-pow((uTime - 2.0) / 6.0, 2.0));
    float wave = exp(-pow((along - uWave.x) / .035, 2.0)) * uWave.y;
    float field = .12 + .20 * flow + .22 * streak * streak;
    float3 color = lerp(uShade, uColor, saturate(flow * .8 + .3)) * field;
    color += uLight * wave * .85;
    color += uColor * engraving * (.10 + .20 * noise);
    color += uLight * (rim * (1.1 + .4 * flow) + spokes * .10 * streak);
    color += uLight * pulse * (.20 + .15 * noise);
    // 实心币中心是铸印亮芯，金币保持内孔完全透明。
    if (uInnerRadius <= 0.0)
    {
        float stamp = exp(-pow(radius / max(uOuterRadius * .15, 1.0), 2.0));
        color += uLight * stamp * (.6 + .4 * pulse);
    }
    float mask = coverage * uEnvelope;
    float alpha = saturate((field * .45 + rim * .55 + wave * .25 + pulse * .10) * mask);
    return float4(saturate(color * mask), alpha);
}

technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 Main();
    }
}
