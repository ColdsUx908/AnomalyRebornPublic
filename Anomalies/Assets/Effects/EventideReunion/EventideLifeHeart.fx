sampler uImage0 : register(s0);
float uCoverage;
float uTime;
float uBar;
float uTexelWidth;

float4 Main(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float4 source = tex2D(uImage0, uv);
    // 横条从右向左填充；使用整张纹理的 UV，缺血裁切不会扩大绿色区域。
    float x = lerp(uv.x, 1.0 - uv.x, uBar);
    float mask = saturate((uCoverage - x) / uTexelWidth + 0.5);
    // 采用旧版 Heart4 的青蓝阴影和浅黄绿主色，保留原图像素的明暗分区。
    float3 rgb = saturate(source.rgb / max(source.a, 0.001));
    float light = 0.85 * max(rgb.r, max(rgb.g, rgb.b)) + 0.15 * min(rgb.r, rgb.g);
    float3 shade = lerp(float3(18, 45, 66), float3(33, 116, 101), smoothstep(0.05, 0.45, light));
    shade = lerp(shade, float3(107, 206, 84), smoothstep(0.38, 0.65, light));
    shade = lerp(shade, float3(209, 255, 127), smoothstep(0.62, 0.83, light));
    float gleam = saturate(0.5 + 0.5 * cos(5.8 * (uv.x + uv.y) - uTime * 2.6));
    shade = lerp(shade, float3(242, 255, 202), smoothstep(0.82, 1.0, light) * (0.65 + 0.2 * gleam));
    return float4(shade * (source.a / 255.0), source.a) * color * mask;
}

technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 Main();
    }
}
