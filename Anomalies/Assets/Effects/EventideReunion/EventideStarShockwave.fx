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

float4 Main(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 position = (uv - 0.5) * (2 * uOuterGeometry.y);
    float radius = length(position);
    float behind = uExpansionRatio - radius;
    float2 direction = position / max(radius, 0.001);
    float2 flow = position * 2.4 - direction * uGlobalTime * 0.65;
    float broad = tex2D(uNoiseSampler, flow * 0.37 + 0.17).r;
    float fine = tex2D(uNoiseSampler, flow * 1.7 + broad * 0.23).r;
    float density = smoothstep(0.2, 0.78, broad * 0.65 + fine * 0.35);
    float width = max(3 / max(uTextureSize.x, 1), min(uExpansionRatio * 0.32, 0.027 + uLifetimeRatio * 0.021));
    float distance = behind / width;
    // 亮脊贴着判定前沿，低密度蒸气在前后两侧连续衰减。
    float ridge = (distance - 0.25 - (broad - 0.5) * 0.28) / 0.44;
    float wake = (distance - 0.84 - (fine - 0.5) * 0.72) / 1.08;
    float crest = exp(-ridge * ridge) * (0.23 + density * 0.34);
    float vapor = exp(-wake * wake) * (0.13 + density * 0.30);
    float3 ice = float3(0.69, 0.80, 0.98);
    float3 gas = lerp(float3(0.18, 0.12, 0.44), float3(0.43, 0.47, 0.79), density);
    float alpha = crest + vapor * (1 - crest);
    return float4(ice * crest + gas * vapor * (1 - crest), alpha) * tint.a;
}
technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 Main();
    }
}
