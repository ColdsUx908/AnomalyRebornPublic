float uTime;
float uRise;
float uRelease;
float uThin;
float uLength;
float uParticleColorIntensity;
sampler uParticleSampler : register(s0);
texture uOuter;
texture uFlow;
texture uStreak;
texture uNoise;
sampler uOuterSampler : register(s1) = sampler_state { Texture = <uOuter>; MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Wrap; AddressV = Clamp; };
sampler uFlowSampler : register(s2) = sampler_state { Texture = <uFlow>; MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Wrap; AddressV = Clamp; };
sampler uStreakSampler : register(s3) = sampler_state { Texture = <uStreak>; MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Wrap; AddressV = Clamp; };
sampler uNoiseSampler : register(s4) = sampler_state { Texture = <uNoise>; MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Wrap; AddressV = Clamp; };

float4 Tile(uniform sampler source, float2 uv)
{
      float x = frac(uv.x);
      float weight = 0.5 - 0.5 * cos(x * 6.2831853);
      return lerp(tex2D(source, float2(frac(x + 0.5), uv.y)), tex2D(source, float2(x, uv.y)), weight);
}

float Noise(float2 uv) { return Tile(uNoiseSampler, uv).r; }

float4 Main(float2 uv : TEXCOORD0, float4 color : COLOR0, uniform int mode) : COLOR0
{
    if (mode == 1)
    {
        float4 particle = tex2D(uParticleSampler, uv) * color;
        return float4(particle.rgb * uParticleColorIntensity, particle.a);
    }
    float axis = abs(uv.x * 2.0 - 1.0);
    float along = axis * uLength * .5;
    // 横截面直接对应判定全宽；噪声只改变内部光流，不缩小危险区。
    float side = 1.0 - uv.y * 2.0;
    float flow = along / 560.0;
    float n = Noise(float2(flow * 1.3 - uTime * .022, side * .38 + .41));
    float warp = (n - .43) * .16;
    float taper = 1.0 - smoothstep(.81, 1.0, axis);
    float radius = pow(max(taper, .001), .35);
    float q = abs(side) / radius;
    float edge = 1.0 - smoothstep(.985, 1.0, q);
    float v = side * .48 + .5;
    float4 outerSample = Tile(uOuterSampler, float2(flow - uTime * .033, v + warp));
    float outer = outerSample.r * outerSample.a;
    float stream = Tile(uFlowSampler, float2(flow * .73 - uTime * .054 + .37, v + warp * .7)).r;
    float streak = Tile(uStreakSampler, float2(flow * 1.42 - uTime * .087, v + warp * .3)).r;
    float fine = Tile(uStreakSampler, float2(flow * 2.1 - uTime * .12 + .61, side * .85 + .5)).r;
    // 同炎爆：纹理亮度决定分层颜色，暗沟、亮脊和窄芯保留各自的覆盖率。
    float ridge = pow(abs(streak), 1.65) * .78 + pow(abs(fine), 2.4) * .32;
    float field = edge * (.10 * outer + .34 * stream + ridge) * exp(-side * side * 2.5);
    float core = exp(-pow(abs(side / (.035 + .025 * stream)), 2.0));
    float contour = smoothstep(.08, .19, field);
    float3 amber = float3(.95, .36, .018);
    float3 gold = float3(2.8, 1.48, .12);
    float3 hot = float3(4.0, 2.4, .36);
    float3 bands = lerp(amber, gold, smoothstep(.12, .40, field));
    bands = lerp(bands, hot, smoothstep(.62, 1.02, field));
    float3 body = bands * contour * (.12 + .88 * smoothstep(.12, .85, field)) * .28;
    float filament = smoothstep(.32, .78, fine) * edge * (1.0 - exp(-side * side * 45.0));
    body += lerp(gold, hot, filament) * filament * .22;
    body += float3(7.0, 5.0, 2.0) * core * (.7 + .3 * stream);
    // 单一束身持续变细；固定空间的多尺度噪声从边缘侵蚀，形成不规则断点。
    float thin = uThin;
    float release = uRelease;
    float signedAlong = (uv.x - .5) * uLength;
    float coarse = Noise(float2(signedAlong / 175.0 + .17, .417));
    float detail = Noise(float2(signedAlong / 43.0 + .61, side * .23 + .52));
    float erode = smoothstep(.24, .65, coarse * .72 + detail * .28);
    float localRadius = max(0.0, 1.0 - release * (1.02 + erode * .95));
    // 归一化到剩余截面：消融只改变覆盖范围，存留的光芯不随半径变暗。
    float remainingQ = q / max(localRadius, .0001);
    float cut = (1.0 - smoothstep(.985, 1.0, remainingQ)) * step(.0001, localRadius);
    float rise = uRise;
    float mask = edge * taper * cut * rise;
    float3 thinBody = lerp(float3(4.0, 2.0, .12), float3(7.8, 6.0, 4.0), exp(-remainingQ * remainingQ * 7.0));
    float3 rgb = lerp(body, thinBody, thin);
    // Canvas 仅传递编码辐亮度，不能在 Bloom 提取前压亮度或混入白色雾层。
    // 首次撑开时达到约三倍能量，随后回到稳定值；退场只减覆盖，不减发光强度。
    rgb *= mask / 8.0; // 爆发倍率在最终合成前解码时统一施加，避免传输溢出。
    float alpha = max(max(rgb.r, rgb.g), rgb.b);
    return float4(rgb, alpha);
}

technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 Main(0);
    }
    pass Particles
    {
        PixelShader = compile ps_3_0 Main(1);
    }
}
