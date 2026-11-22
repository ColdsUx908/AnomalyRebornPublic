float2 uExtent;
float uTime;
float uTrail;

float4 Main(float2 uv : TEXCOORD0, float4 tint : COLOR0, uniform float dash) : COLOR0
{
    float2 p = (uv - 0.5) * uExtent;
    float prism = 0.0;
    float gold;
    float white;
    if (dash > 0.5)
    {
        // World-pixel capsule around the caster, with a narrowing wake behind it.
        float2 head = p - float2(uTrail * 0.5, 0.0);
        float wake = saturate(1.0 + head.x / max(uTrail, 1.0));
        float envelope = saturate(-head.x / 13.0) * smoothstep(0.0, 0.14, wake);
        float ribbon = head.y + sin(head.x * 0.043 + uTime * 32.0) * 4.0 * (1.0 - wake);
        float body = length(head / float2(59.0, 63.0));
        gold = max(1.0 - smoothstep(0.45, 1.15, body),
            envelope * (1.0 - smoothstep(1.0, 5.0 + 49.0 * wake, abs(ribbon))));
        white = max(1.0 - smoothstep(0.5, 0.87, body),
            envelope * (1.0 - smoothstep(1.0, 3.0 + 23.0 * wake, abs(ribbon))) * wake);
        float phase = head.x * 0.034 + uTime * 27.0;
        float orbit = (11.0 + 43.0 * wake) * sin(phase);
        float separation = min(abs(head.y - orbit), abs(head.y + orbit));
        prism = envelope * wake * (1.0 - smoothstep(1.2, 5.5, separation));
    }
    else
    {
        // Local world pixels keep the descending pattern anchored while the camera moves.
        float flow = p.y * 0.032 - uTime * 16.0;
        float radius = abs(p.x) / (uExtent.x * 0.25);
        float braid = abs(abs(p.x / (uExtent.x * 0.25) + sin(flow * 0.57) * 0.04) - (0.57 + 0.25 * sin(flow)));
        float thread = 1.0 - smoothstep(0.014, 0.07, braid);
        float veil = 1.0 - smoothstep(0.6, 1.85, radius);
        gold = veil * (0.52 + 0.20 * sin(flow + p.x * 0.08)) + thread * 0.6;
        white = (1.0 - smoothstep(0.20, 0.96, radius)) * 0.94 + thread * 0.35;
        // Geometry extends one beam radius past each damage endpoint; fade only in that margin.
        float endFade = smoothstep(0.0, uExtent.x * 0.25, uExtent.y * 0.5 - abs(p.y));
        gold *= endFade;
        white *= endFade;
    }
    float3 color = lerp(float3(1.0, 0.73, 0.32), float3(1.0, 1.0, 0.99), saturate(white));
    float3 light = color * saturate(max(gold, white));
    if (dash > 0.5)
    {
        float3 silk = lerp(float3(0.48, 0.87, 1.0), float3(1.0, 0.61, 0.85), 0.5 + 0.5 * sin(p.x * 0.027 + uTime * 15.0));
        light = lerp(light, silk, prism * (1.0 - 0.8 * white));
    }
    return float4(light * tint.rgb, 0.0);
}

technique Technique1
{
    pass Pass0 { PixelShader = compile ps_3_0 Main(0.0); }
    pass Dash { PixelShader = compile ps_3_0 Main(1.0); }
}
