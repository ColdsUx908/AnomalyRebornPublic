// Emissive stellar photosphere and corona. Square local UVs are world-isotropic.
texture uNoiseTexture;
sampler uNebulaSampler : register(s1) = sampler_state
{
    Texture = <uNoiseTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
float uTime;
float uCollapse;

float Noise3(float3 position)
{
    float a = tex2D(uNebulaSampler, position.xy + position.z * float2(0.17, -0.11)).r;
    float b = tex2D(uNebulaSampler, position.yz + position.x * float2(-0.13, 0.19) + 0.37).r;
    float c = tex2D(uNebulaSampler, position.zx + position.y * float2(0.21, 0.09) + 0.71).r;
    return saturate(((a * 0.5 + b * 0.3 + c * 0.2) - 0.48) * 2.5 + 0.5);
}

float4 Main(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, uniform bool solar) : COLOR0
{
    float2 disk = (coords - 0.5) * 3.5;
    float radius = length(disk);
    float facing = sqrt(max(0.0001, 1 - dot(disk, disk)));
    float3 normal = float3(disk, facing);
    float3 tilted = float3(normal.x, normal.y * 0.94 + normal.z * 0.342,
        normal.z * 0.94 - normal.y * 0.342);
    float spin = uTime * 0.85;
    float sine = sin(spin);
    float cosine = cos(spin);
    float3 surface = float3(tilted.x * cosine + tilted.z * sine, tilted.y,
        tilted.z * cosine - tilted.x * sine);
    float time = uTime * 0.025;
    // Low frequency domain warp forms rolling convection, rather than sand-like speckles.
    float3 warp = float3(Noise3(surface * 0.32 + time),
        Noise3(surface * 0.32 + 4.7 - time), Noise3(surface * 0.32 + 9.2 + time));
    float3 position = surface * 0.72 + (warp - 0.5) * 0.28;
    float broad = Noise3(position + time * 0.2);
    float medium = Noise3(position * 2.1 - time * 0.35 + 2.4);
    float fine = Noise3(position * 4.7 + 7.3 + time * 0.55);
    float plasma = saturate(broad * 0.57 + medium * 0.3 + fine * 0.13);
    float filamentShape = saturate(1 - abs(medium - 0.54) * 9);
    float filaments = filamentShape * filamentShape * filamentShape * smoothstep(0.37, 0.68, broad);
    float hotRegions = smoothstep(0.44, 0.76, plasma);
    float3 violet = solar ? float3(0.46, 0.095, 0.018) : float3(0.19, 0.07, 0.46);
    float3 blue = solar ? float3(0.94, 0.37, 0.045) : float3(0.20, 0.38, 0.92);
    float3 ice = solar ? float3(1.0, 0.82, 0.28) : float3(0.60, 0.77, 1.0);
    float3 whiteHot = solar ? float3(1.0, 0.96, 0.74) : float3(0.96, 0.97, 1.0);
    float3 color = lerp(violet, blue, smoothstep(0.18, 0.52, plasma));
    color = lerp(color, ice, hotRegions);
    float whiteRegions = smoothstep(0.53, 0.79, broad) * smoothstep(0.42, 0.73, medium);
    color = lerp(color, whiteHot, saturate(whiteRegions * 0.82 + filaments * 0.76));
    // Keep self-emission at the limb; a hot chromosphere provides the bright reference silhouette.
    color *= 0.63 + 0.37 * facing;
    float chromosphere = exp(-abs(radius - 0.993) * 115);
    color = lerp(color, whiteHot, saturate(chromosphere * 0.86 + uCollapse * 0.65));
    float mask = (1 - smoothstep(0.995, 1.008, radius));
    float angle = atan2(disk.y, disk.x);
    float2 coronaUV = float2(angle / 6.2831853, radius * 0.7);
    float coronaNoise = tex2D(uNebulaSampler, coronaUV * float2(3, 1) + float2(time * 0.13, -time)).r;
    float tendrilShape = saturate(tex2D(uNebulaSampler, coronaUV * float2(7, 0.7)
        + float2(-time * 0.07, -time * 0.6)).r * 1.5 - 0.28);
    float tendrils = tendrilShape * tendrilShape * tendrilShape;
    float height = solar ? 0.05 + tendrils * 0.32 : 0.025 + tendrils * 0.17;
    float outside = max(radius - 1, 0);
    float flames = exp(-outside / height * 3.2) * (0.4 + tendrils * 2.3);
    float halo = exp(-outside * 5.2) * 0.24 + exp(-outside * 11) * 0.25;
    if (solar)
    {
        // 日冕内侧连成金亮的火冠，外侧随磁束分成不等长的细流。
        float crown = saturate(coronaNoise * 0.68 + tendrilShape * 0.46);
        float streamer = pow(crown, 3) * exp(-outside * (3.4 + 4.0 * (1 - crown)));
        flames = exp(-outside / (0.075 + 0.29 * crown)) * (0.42 + 0.83 * tendrilShape) + streamer;
        halo = 0.36 * exp(-outside * 5);
    }
    float corona = (flames + halo) * smoothstep(0.975, 1.015, radius)
        * (1 - smoothstep(1.45, 1.72, radius));
    float3 coronaColor = lerp(solar ? float3(0.97, 0.23, 0.035) : float3(0.49, 0.25, 0.89), ice, exp(-outside * (solar ? 5.5 : 17)));
    // Sparse magnetic arches, with independent heights and lifetimes, rise above the limb.
    float loopAngle = angle / 6.2831853 * 11 + spin * 0.12;
    float sector = floor(loopAngle + 0.5);
    sector -= floor(sector / 11) * 11; // Same prominence on both sides of the atan2 seam.
    float localAngle = frac(loopAngle + 0.5) - 0.5;
    float seed = frac(sin(sector * 127.1 + 19.7) * 43758.5453);
    float width = 0.12 + seed * 0.08;
    float archShape = sqrt(max(0.0001, 1 - localAngle * localAngle / (width * width)));
    float archRadius = 1.012 + archShape * (solar ? 0.16 + seed * 0.27 : 0.07 + seed * 0.1)
        + sin(localAngle * 70 + seed * 15 + time) * archShape * 0.006;
    float arch = exp(-abs(radius - archRadius) * (solar ? 68 : 140))
        * (1 - smoothstep(width * 0.85, width, abs(localAngle)))
        * smoothstep(0.05, 0.4, sin(uTime * 0.45 + sector * 2.4))
        * smoothstep(1, 1.025, radius);
    float archLight = arch * (0.5 + coronaNoise * 0.5);
    float alpha = saturate(mask + corona + archLight);
    float3 emission = color * mask + coronaColor * corona + ice * archLight;
    return float4(min(emission, alpha) * sampleColor.rgb, alpha * sampleColor.a);
}
technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 Main(false);
    }
    pass SolarFlare
    {
        PixelShader = compile ps_3_0 Main(true);
    }
}
