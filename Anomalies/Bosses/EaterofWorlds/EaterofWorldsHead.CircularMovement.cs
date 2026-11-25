namespace Anomalies.Bosses.EaterofWorlds;

public sealed partial class EaterofWorldsHead
{
    // The vanilla frames extend 23 opaque source pixels to each side of a body joint.
    // Scale the two anchors once, with extra overlap at the neck of every local head.
    internal static float LinkSpacing(NPC front, NPC rear) =>
        (front.type == NPCID.EaterofWorldsHead ? 15f : 18f) * front.scale + 17f * rear.scale;

    // Per-local-head synchronized slots. Radius zero selects articulated following.
    public bool CircularMovementActive => AnomalyNPC.AnomalyAI32[2].F > 0f;
    public float CircularRadius => AnomalyNPC.AnomalyAI32[2].F;
    public float CircularAngularSpeed => AnomalyNPC.AnomalyAI32[3].F;
    public Vector2 CircularCenter => AnomalyNPC.AnomalyAI64[2].GetValue<Vector2>();
    public float CircularHeadAngle => AnomalyNPC.AnomalyAI32[5].F;

    /// <summary>
    /// 为当前局部虫链启用圆弧运动。角速度单位为弧度/帧，正值顺时针。
    /// 体节按累计弧长排列，不逐节追赶；首次启用会重排身体，应在无接触伤害的转场中调用。
    /// 仅服务器设置参数；客户端沿用同步状态预测。
    /// </summary>
    public bool StartCircularMovement(Vector2 center, float radius, float headAngle, float angularSpeed)
    {
        if (!TONetworkEnvironment.NotClient || !float.IsFinite(center.X) || !float.IsFinite(center.Y)
            || !float.IsFinite(radius) || radius <= 0f || !float.IsFinite(headAngle)
            || !float.IsFinite(angularSpeed) || angularSpeed == 0f || Math.Abs(angularSpeed) > MathHelper.Pi
            || !TryGetCircularChain(out List<NPC> chain))
            return false;

        // A full turn would erase the opening. Reject it instead of silently compressing the worm.
        float length = 0f;
        for (int i = 1; i < chain.Count; i++)
            length += LinkSpacing(chain[i - 1], chain[i]);
        if (!float.IsFinite(length) || length <= 0f || length >= MathHelper.TwoPi * radius)
            return false;

        AnomalyNPC.AnomalyAI32[2].F = radius;
        AnomalyNPC.AnomalyAI32[3].F = angularSpeed;
        AnomalyNPC.AnomalyAI64[2].SetValue(center);
        AnomalyNPC.AIChanged32[2] = AnomalyNPC.AIChanged32[3] = true;
        AnomalyNPC.AIChanged64[2] = true;
        AnomalyNPC.AnomalyAI32[5].F = MathHelper.WrapAngle(headAngle);
        AnomalyNPC.AIChanged32[5] = true;
        NPC.netUpdate = true;
        PlaceCircularChain(chain);
        return true;
    }

    /// <summary>
    /// 服务器停止当前局部虫链的圆弧运动，恢复基础跟随。
    /// </summary>
    public void StopCircularMovement()
    {
        if (TONetworkEnvironment.NotClient)
            ClearCircularMovement();
    }

    private void ClearCircularMovement()
    {
        if (!CircularMovementActive)
            return;
        AnomalyNPC.AnomalyAI32[2].F = 0f;
        AnomalyNPC.AnomalyAI32[3].F = 0f;
        AnomalyNPC.AnomalyAI64[2].SetValue(Vector2.Zero);
        AnomalyNPC.AIChanged32[2] = AnomalyNPC.AIChanged32[3] = true;
        AnomalyNPC.AIChanged64[2] = true;
        AnomalyNPC.AnomalyAI32[5].F = 0f;
        AnomalyNPC.AIChanged32[5] = true;
        NPC.netUpdate |= TONetworkEnvironment.NotClient;
    }

    private void StopEncounterCircularMovement()
    {
        foreach (NPC head in GetEncounterHeads())
            GetInstance(head).ClearCircularMovement();
    }

    private void UpdateCircularMovement(List<NPC> heads)
    {
        // Only the encounter owner advances and places each local chain. Individual segments never
        // integrate a second velocity, so final geometry does not depend on NPC slot/update order.
        foreach (NPC head in heads)
        {
            EaterofWorldsHead behavior = GetInstance(head);
            if (!behavior.CircularMovementActive || !behavior.TryGetCircularChain(out List<NPC> chain))
                continue;
            behavior.AnomalyNPC.AnomalyAI32[5].F = MathHelper.WrapAngle(behavior.CircularHeadAngle + behavior.CircularAngularSpeed);
            behavior.PlaceCircularChain(chain);
            if (TONetworkEnvironment.NotClient && Main.GameUpdateCount % 60 == 0)
            {
                behavior.AnomalyNPC.AIChanged32[5] = true;
                head.netUpdate = true;
            }
        }
    }

    private bool TryGetCircularChain(out List<NPC> chain)
    {
        chain = [];
        if (!NPC.active || NPC.type != NPCID.EaterofWorldsHead || LocalHead != NPC || !ValidHead)
            return false;
        NPC previous = NPC.DummyNPC;
        NPC current = NPC;
        int ordinal = SegmentIndex;
        while (current != NPC.DummyNPC && current is not null && chain.Count < Main.maxNPCs)
        {
            IEaterofWorldsSegment segment = EaterofWorldsHandler.TryGetSegment(current);
            if (!current.active || segment is null || segment.Head != Head || segment.LocalHead != NPC
                || segment.Previous != previous || segment.SegmentIndex != ordinal++)
                return false;
            chain.Add(current);
            previous = current;
            current = segment.Next;
        }
        return current == NPC.DummyNPC && chain.Count >= 2;
    }

    private void PlaceCircularChain(List<NPC> chain)
    {
        float direction = Math.Sign(CircularAngularSpeed);
        float distance = 0f;
        for (int i = 0; i < chain.Count; i++)
        {
            NPC segment = chain[i];
            if (i > 0)
                distance += LinkSpacing(chain[i - 1], segment);
            float angle = CircularHeadAngle - direction * distance / CircularRadius;
            Vector2 radial = new(MathF.Cos(angle), MathF.Sin(angle));
            segment.Center = CircularCenter + radial * CircularRadius;
            segment.velocity = Vector2.Zero;
            segment.rotation = angle + direction * MathHelper.PiOver2 + MathHelper.PiOver2;
        }
    }
}
