using Microsoft.Xna.Framework.Audio;
using NVorbis;

namespace Anomalies.Bosses.Skeletron.LegendaryNight;

public sealed class SkeletronLegendMusicSystem : ModSystem
{
    // 当前 tML 的音轨接口没有定位方法。只在一次性中途加入定位时访问 Vorbis 解码器。
    private FieldInfo _VorbisReaderField;
    private NPC _JoinHead;

    private Func<bool> _OverrideSkeletron;
    private int _DungeonMusic = -1;
    internal int DungeonMusic => _OverrideSkeletron?.Invoke() == true ? _DungeonMusic : -1;
    internal int Phase2Music { get; private set; } = -1;

    public override void PostSetupContent()
    {
        if (Main.dedServ)
            return;

        if (ModLoader.TryGetMod("ClamExtraMusic", out Mod extraMusic)
            && extraMusic.TryFind("PriorityConfig", out Terraria.ModLoader.Config.ModConfig config)
            && config.GetType().GetProperty("OverrideSkeletron")?.GetMethod is MethodInfo getter
            && getter.ReturnType == typeof(bool) && !getter.IsStatic && getter.GetParameters().Length == 0)
        {
            // 传说之夜使用独立 NPC，直接沿用外部模组对原版骷髅王的实时选曲开关。
            _OverrideSkeletron = getter.CreateDelegate<Func<bool>>(config);
            int slot = MusicLoader.GetMusicSlot(extraMusic, "Assets/Music/CurseOfTheDungeon");
            _DungeonMusic = slot > 0 ? slot : -1;
        }

        if (ModLoader.TryGetMod("LegendaryNightExtraMusic", out Mod privateMusic))
        {
            int slot = MusicLoader.GetMusicSlot(privateMusic, "Music/TornfromtheHeavens");
            Phase2Music = slot > 0 ? slot : -1;
        }
    }

    public override void Load()
    {
        if (Main.dedServ)
            return;

        _VorbisReaderField = typeof(OGGAudioTrack).GetField("_vorbisReader", BindingFlags.Instance | BindingFlags.NonPublic);
        On_Main.UpdateAudio_DecideOnNewMusic += DecideOnNewMusic;
        On_Main.UpdateAudio_DecideOnTOWMusic += DecideOnTOWMusic;
        On_LegacyAudioSystem.UpdateCommonTrack += SyncJoinedTrack;
    }

    public override void OnWorldUnload() => _JoinHead = null;

    internal void CaptureJoinEncounter()
    {
        _JoinHead = Main.netMode == NetmodeID.MultiplayerClient
            && LegendaryNightHandler.TryGetSceneHead(out SkeletronLegendHead head)
            && head.Phase2MusicActive && head.Phase2MusicElapsedTime > 0
            ? head.NPC
            : null;
    }

    public override void PostUpdateNPCs()
    {
        if (_JoinHead is not null && (!LegendaryNightHandler.TryGetSceneCandidate(_JoinHead, out SkeletronLegendHead head)
            || !head.Phase2MusicActive))
            _JoinHead = null;
    }

    private void SyncJoinedTrack(On_LegacyAudioSystem.orig_UpdateCommonTrack orig, LegacyAudioSystem audio, bool active,
        int trackIndex, float totalVolume, ref float fade)
    {
        orig(audio, active, trackIndex, totalVolume, ref fade);

        if (_JoinHead is null || !active || trackIndex != Phase2Music
            || !LegendaryNightHandler.TryGetSceneHead(out SkeletronLegendHead head) || head.NPC != _JoinHead
            || audio.AudioTracks[trackIndex] is not { IsPlaying: true } track)
            return;

        _JoinHead = null;
        if (track is not OGGAudioTrack ogg || _VorbisReaderField?.GetValue(ogg) is not VorbisReader reader || reader.TotalSamples <= 0)
            return;

        long offset = (long)head.Phase2MusicElapsedTime * reader.SampleRate / 60 % reader.TotalSamples;
        reader.SeekTo(offset);
        // 丢弃已排队的旧位置 PCM，随本次音轨更新从定位后的解码位置开始播放。
        track.Stop(AudioStopOptions.Immediate);
        track.Play();
    }

    private static void DecideOnNewMusic(On_Main.orig_UpdateAudio_DecideOnNewMusic orig, Main self)
    {
        orig(self);
        ApplyLegendaryNightMusic();
    }
    private static void DecideOnTOWMusic(On_Main.orig_UpdateAudio_DecideOnTOWMusic orig, Main self)
    {
        orig(self);
        ApplyLegendaryNightMusic();
    }
    private static void ApplyLegendaryNightMusic()
    {
        if (Main.dedServ || Main.gameMenu)
            return;

        // 场景优先级仍可能被其他骷髅王音乐覆盖，在音乐选择结束后应用当前阶段。
        // 复用场景检查，保留距离限制、转场时机和音乐包未加载时的回退。
        SkeletronLegendScene2 phase2Scene = ModContent.GetInstance<SkeletronLegendScene2>();
        if (phase2Scene.IsSceneEffectActive(Main.LocalPlayer))
        {
            Main.newMusic = phase2Scene.Music;
            return;
        }

        SkeletronLegendScene phase1Scene = ModContent.GetInstance<SkeletronLegendScene>();
        if (phase1Scene.IsSceneEffectActive(Main.LocalPlayer))
            Main.newMusic = phase1Scene.Music;
    }
}

public sealed class LegendaryNightMusicJoinBehavior : AnomalyPlayerBehavior
{
    public override void OnEnterWorld() => ModContent.GetInstance<SkeletronLegendMusicSystem>().CaptureJoinEncounter();
}
