namespace Anomalies.Bosses.EaterofWorlds;

// Immutable topology plan: global order survives every split; local edges never cross worm boundaries.
internal static class EaterofWorldsTopology
{
    internal readonly record struct Link(int Head, int Previous, int Next);

    internal static Link[] Create(int segmentCount, int wormCount)
    {
        if (wormCount <= 0 || segmentCount < 2 || segmentCount % wormCount != 0 || segmentCount / wormCount < 2)
            return [];
        int length = segmentCount / wormCount;
        Link[] links = new Link[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            int offset = i % length;
            links[i] = new(i - offset, offset == 0 ? -1 : i - 1, offset == length - 1 ? -1 : i + 1);
        }
        return links;
    }

    internal static Link[] Create(int segmentCount, System.ReadOnlySpan<int> lengths)
    {
        if (segmentCount < 2 || lengths.IsEmpty)
            return [];
        int remaining = segmentCount;
        foreach (int length in lengths)
        {
            if (length < 2 || length > remaining)
                return [];
            remaining -= length;
        }
        if (remaining != 0)
            return [];

        Link[] links = new Link[segmentCount];
        int start = 0;
        foreach (int length in lengths)
        {
            int end = start + length;
            for (int i = start; i < end; i++)
                links[i] = new(start, i == start ? -1 : i - 1, i == end - 1 ? -1 : i + 1);
            start = end;
        }
        return links;
    }
}
