// Inspired by CrossMod Eternity Ares GaussNukeBoom's filled blast and bright rim.
// Reference: Fargo-Team/FargosSoulsDLC, GaussNukeExplosionShader (MIT, Javyz 2023).
// License: Anomalies/Licenses/FargowiltasCrossmod-MIT.txt.
// Sprite-local square UVs represent equal world distances on both axes.
texture uNoiseTexture;
sampler uNoiseSampler : register(s1) = sampler_state
{
    Texture = <uNoiseTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
float uGlobalTime;
float uLifetimeRatio;
// Failure: expansion, blast transition, erosion, dilution. Success: unused, full sprite opacity, unused, outer fade.
float4 uRemnantState;
float3 uInteriorState; // Outward drift, erosion threshold, remaining density.
float2 uOuterGeometry; // Outer front radius and sprite padding, in units of the original blast radius.
float uExpansionRatio;
float uYellowPalette;
float2 uTextureSize;

float3 DrawRemnant(float2 position, out float coverage)
{
    float radius = length(position);
    float drift = uInteriorState.x;
    float2 direction = position / max(radius, 0.001);
    // 同一方向上的云纹持续向外平流，相邻方向的抛射速度不同。
    float plume = tex2D(uNoiseSampler, direction * 0.36 + 0.43).r;
    float stretch = drift * (0.08 + 0.48 * plume) * smoothstep(0.22, 0.94, radius);
    float2 p = position / (1 + stretch);
    float2 warp = float2(tex2D(uNoiseSampler, p * 0.68 + 0.21).r,
                         tex2D(uNoiseSampler, p * 0.68 + 0.63).r) - 0.5;
    float2 flow = p + warp * 0.19 + drift * float2(-0.045, 0.017);
    float broad = tex2D(uNoiseSampler, flow * 0.94 + 0.27).r;
    float folds = tex2D(uNoiseSampler, flow * 2.7 + broad * 0.23).r;
    float grain = tex2D(uNoiseSampler, flow * 8.1 + folds * 0.12).r;
    float density = broad * 0.59 + folds * 0.30 + grain * 0.11;
    // 斜向亮带从偏心云核发散，暗尘边缘截住光照并露出粉紫色受光面。
    float lane = dot(p, float2(0.47, 0.88)) + warp.y * 0.47;
    float stream = exp(-lane * lane * 8.5);
    float2 offset = p - float2(-0.09, 0.045) + warp * 0.09;
    float core = exp(-dot(offset, offset) * 11) * stream;
    float dust = density + lane * 0.20 - 0.48;
    float diffuse = tex2D(uNoiseSampler, p * 0.32 + 0.37 + drift * 0.025).r;
    float light = smoothstep(-0.22, 0.18, dust);
    float rim = exp(-dust * dust * 210) * (0.30 + stream * 0.70);
    float cloud = saturate(0.22 + (density - 0.26) * 1.4);
    float3 gas = lerp(float3(0.055,0.085,0.30), float3(0.14,0.29,0.74),cloud) * cloud;
    gas += float3(0.12,0.58,0.89) * stream * light * (0.27 + core * 0.75);
    gas += float3(0.88,0.40,0.69) * rim * (0.30 + folds * 0.45);
    gas += float3(0.90,0.88,1.0) * core * (0.38 + light * 0.25);
    float breakup = drift;
    float contour = length(p) - (plume - 0.5) * (0.34 + breakup * 0.36)
        - (broad - 0.5) * 0.20 - rim * (0.08 + 0.16 * drift);
    float edge = 1 - smoothstep(0.62, 1.03, contour);
    float filaments = saturate(rim + 0.40 * broad);
    coverage = edge * lerp(1, filaments, breakup);

    float survival = smoothstep(-0.28,0.24,density - uRemnantState.z);
    // 弥散云气沿同一条云带穿过低密度区，比细丝更缓慢地消散。
    float haze = (0.16 + 0.54 * diffuse) * (0.30 + 0.70 * stream) * (0.45 + 0.55 * core);
    float3 scattered = lerp(float3(0.18,0.28,0.56), float3(0.46,0.66,0.86), diffuse) * haze;
    return (gas * lerp(survival, 0.4 + 0.6 * survival, core) + scattered) * coverage * uInteriorState.z

        + float3(0.68,0.27,0.58) * rim * plume * coverage * drift * uInteriorState.z;
}
float3 StarLayer(float2 position, float frequency, float seedOffset, float drift)
{
    float2 grid = position * frequency / (1 + drift) + seedOffset;
    float2 cell = floor(grid);
    float2 seed = frac(sin(float2(dot(cell,float2(41.17,59.81)),dot(cell,float2(73.03,29.57)))) * 4375.63);
    float2 offset = frac(grid) - lerp(0.19,0.81,seed);
    float2 square = offset * offset;
    float distanceSq = square.x + square.y;
    float core = exp(-distanceSq * lerp(1500,320,seed.y));
    float halo = exp(-distanceSq * 105) * 0.12;
    float rays = exp(-dot(square,float2(110,1400))) + exp(-dot(square,float2(1400,110)));
    float bright = smoothstep(0.86,0.99,seed.x);
    float twinkle = 0.76 + 0.24 * sin(uGlobalTime * (1.6 + seed.y * 1.8) + seed.x * 18);
    float3 color = lerp(float3(0.44,0.62,1),float3(1,0.74,0.94),seed.y * seed.y);
    return color * (core + halo + rays * bright * 0.95) * twinkle * (0.18 + seed.x * 0.82);
}

float4 Main(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = (uv - 0.5) * (2 * uOuterGeometry.y);
    float coverage;
    float3 gas = DrawRemnant(p, coverage);
    float3 nebula = gas;
    float drift = uInteriorState.x;
    float3 field = StarLayer(p, 41, 7.31, drift * 0.06) * 0.74
                 + StarLayer(p, 16, 19.73, drift * 0.18);
    float window = coverage * uExpansionRatio;
    float density = saturate(gas.b * 1.6 + 0.62);
    float3 emission = saturate(nebula + field * density * window * uInteriorState.z);
    // 深靛色云体对背景产生局部消光，透明区域与消散时的消光同步归零。
    float extinction = saturate(coverage * uInteriorState.z * (0.34 + 0.36 * density));
    float alpha = max(extinction, max(emission.r, max(emission.g, emission.b)));
    return float4(emission, alpha) * tint.a;
}

technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 Main();
    }
}
