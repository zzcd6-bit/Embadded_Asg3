using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 音乐音效管理器。
/// 使用 ResMgr 从 Resources 加载音频。
///
/// BGM 路径：
/// Assets/Resources/music/xxx.wav
/// 调用：PlayBKMusic("xxx")
///
/// 音效路径：
/// Assets/Resources/sound/xxx.wav
/// 调用：PlaySound("xxx")
/// </summary>
public class MusicMgr : BaseMgr<MusicMgr>
{
    private AudioSource bkMusic;

    private float bkMusicValue = 0.5f;

    private readonly List<AudioSource> soundList = new List<AudioSource>();

    private float soundValue = 0.8f;

    private bool soundIsPlay = true;

    private MusicMgr()
    {
        MonoMgr.Instance.AddFixedUpdateListener(Update);
    }

    private void Update()
    {
        if (!soundIsPlay) return;

        for (int i = soundList.Count - 1; i >= 0; i--)
        {
            AudioSource source = soundList[i];

            if (source == null)
            {
                soundList.RemoveAt(i);
                continue;
            }

            if (!source.isPlaying)
            {
                source.clip = null;
                PoolMgr.Instance.PushObj(source.gameObject);
                soundList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 播放背景音乐。
    /// name 是 Resources/music/ 下的音频名字，不需要后缀。
    /// </summary>
    public void PlayBKMusic(string name, bool isSync = false)
    {
        if (string.IsNullOrEmpty(name)) return;

        if (bkMusic == null)
        {
            GameObject obj = new GameObject("BKMusic");
            GameObject.DontDestroyOnLoad(obj);

            bkMusic = obj.AddComponent<AudioSource>();
        }

        string path = "music/" + name;

        LoadAudioClip(
            path,
            isSync,
            (clip) =>
            {
                if (clip == null)
                {
                    Debug.LogError("MusicMgr: BGM 加载失败：" + path);
                    return;
                }

                bkMusic.clip = clip;
                bkMusic.loop = true;
                bkMusic.volume = bkMusicValue;
                bkMusic.Play();
            }
        );
    }

    public void StopBKMusic()
    {
        if (bkMusic == null) return;

        bkMusic.Stop();
    }

    public void PauseBKMusic()
    {
        if (bkMusic == null) return;

        bkMusic.Pause();
    }

    public void ResumeBKMusic()
    {
        if (bkMusic == null) return;

        bkMusic.UnPause();
    }

    public void ChangeBKMusicValue(float value)
    {
        bkMusicValue = Mathf.Clamp01(value);

        if (bkMusic != null)
        {
            bkMusic.volume = bkMusicValue;
        }
    }

    /// <summary>
    /// 播放音效。
    /// name 是 Resources/sound/ 下的音频名字，不需要后缀。
    /// </summary>
    public void PlaySound(
        string name,
        bool isLoop = false,
        bool isSync = false,
        UnityAction<AudioSource> callBack = null
    )
    {
        if (string.IsNullOrEmpty(name)) return;

        string path = "Audio/" + name;

        LoadAudioClip(
            path,
            isSync,
            (clip) =>
            {
                if (clip == null)
                {
                    Debug.LogError("MusicMgr: 音效加载失败：" + path);
                    return;
                }

                GameObject soundObj = PoolMgr.Instance.GetObj("Sound/soundObj");

                if (soundObj == null)
                {
                    Debug.LogError("MusicMgr: 没有找到对象池音效对象 Sound/soundObj");
                    return;
                }

                AudioSource source = soundObj.GetComponent<AudioSource>();

                if (source == null)
                {
                    source = soundObj.AddComponent<AudioSource>();
                }

                source.Stop();
                source.clip = clip;
                source.loop = isLoop;
                source.volume = soundValue;
                source.Play();

                if (!soundList.Contains(source))
                {
                    soundList.Add(source);
                }

                callBack?.Invoke(source);
            }
        );
    }

    /// <summary>
    /// 停止指定音效。
    /// 主要用于停止循环音效，比如蓄力音效。
    /// </summary>
    public void StopSound(AudioSource source)
    {
        if (source == null) return;

        source.Stop();
        source.clip = null;

        if (soundList.Contains(source))
        {
            soundList.Remove(source);
        }

        PoolMgr.Instance.PushObj(source.gameObject);
    }

    public void ChangeSoundValue(float value)
    {
        soundValue = Mathf.Clamp01(value);

        for (int i = 0; i < soundList.Count; i++)
        {
            if (soundList[i] != null)
            {
                soundList[i].volume = soundValue;
            }
        }
    }

    public void PlayOrPauseSound(bool isPlay)
    {
        soundIsPlay = isPlay;

        for (int i = 0; i < soundList.Count; i++)
        {
            AudioSource source = soundList[i];

            if (source == null) continue;

            if (isPlay)
            {
                source.UnPause();
            }
            else
            {
                source.Pause();
            }
        }
    }

    /// <summary>
    /// 清空所有音效。
    /// 重启场景 / 清空对象池前建议调用。
    /// </summary>
    public void ClearSound()
    {
        for (int i = 0; i < soundList.Count; i++)
        {
            AudioSource source = soundList[i];

            if (source == null) continue;

            source.Stop();
            source.clip = null;

            PoolMgr.Instance.PushObj(source.gameObject);
        }

        soundList.Clear();
    }

    private void LoadAudioClip(
        string path,
        bool isSync,
        UnityAction<AudioClip> callBack
    )
    {
        if (isSync)
        {
            AudioClip clip = ResMgr.Instance.Load<AudioClip>(path);
            callBack?.Invoke(clip);
        }
        else
        {
            ResMgr.Instance.LoadAsync<AudioClip>(
                path,
                (clip) =>
                {
                    callBack?.Invoke(clip);
                }
            );
        }
    }
}