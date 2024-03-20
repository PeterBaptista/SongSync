using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.IO;
using UnityEngine.Networking;
using System;
using UnityEditor;
using System.Threading.Tasks;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance;
    public AudioSource audioSource;
    public AudioSource[] audioSources = new AudioSource[3];
    public Lane[] lanes;
    public float songDelayInSeconds;
    public int inputDelayInMilliseconds;
    public double marginOfError;
    public int test;
    public ICollection<Melanchall.DryWetMidi.Interaction.Note> notes;
    public float minLongNoteDuration;

    public int played = 0;

    public string fileLocation;
    public float noteTime;
    public float noteSpawnY;
    public float noteTapY;
    public float noteDespawnY
    {
        get
        {
            return noteTapY - (noteSpawnY - noteTapY);
        }
    }
    public float noteDistanceToTap
    {
        get
        {
            return Mathf.Abs(noteTapY - noteSpawnY);
        }
    }

    public static MidiFile midiFile;
    // Start is called before the first frame update
    async void Start()
    {
        Instance = this;
        if (Application.streamingAssetsPath.StartsWith("http://") || Application.streamingAssetsPath.StartsWith("https://"))
        {
            StartCoroutine(ReadFromWebsite());
        }
        else
        {
            await ReadFromFile();
        }
    }

    private IEnumerator ReadFromWebsite()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(fileLocation))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                byte[] results = www.downloadHandler.data;
                using (var stream = new MemoryStream(results))
                {
                    midiFile = MidiFile.Read(stream);
                    GetDataFromMidi();
                }
            }
        }
    }

    private async Task ReadFromFile()
    {
        string songFolder = PlayerPrefs.GetString("song-folder");
        fileLocation = "Assets/Audio/" + songFolder +"/notes.mid";
        midiFile = MidiFile.Read(fileLocation);
        Debug.Log(fileLocation);

        await Import(songFolder);
        GetDataFromMidi();
    }

    async Task Import(string songFolder)
    {
        string guitarPath = Directory.GetCurrentDirectory() + "/Assets/Audio/" + songFolder + "/guitar.ogg";
        string songPath = Directory.GetCurrentDirectory() + "/Assets/Audio/" + songFolder + "/song.ogg";
        string rhythmPath = Directory.GetCurrentDirectory() + "/Assets/Audio/" + songFolder + "/rhythm.ogg";

        if (!File.Exists(guitarPath))
        {
            Debug.LogError("File not found: " + guitarPath);
            return;
        }
        /* if (!File.Exists(songPath))
        {
            Debug.LogError("File not found: " + songPath);
            return;
        }
         if (!File.Exists(rhythmPath))
        {
            Debug.LogError("File not found: " + rhythmPath);
            return;
        } */
        
        AudioClip songClip = await LoadClip(guitarPath);
        AudioClip songClip2 = await LoadClip(songPath);
        AudioClip songClip3 = await LoadClip(rhythmPath);

        if (songClip != null)
        {
            Debug.Log("Song imported successfully: " + songClip.name);

            Instance.audioSources[0].clip = songClip;
            Instance.audioSources[1].clip = songClip2;
            Instance.audioSources[2].clip = songClip3;

            Instance.audioSource.clip = songClip;
        }
        else
        {
            Debug.LogError("Failed to import song");
        }

        return;
    }

    async Task<AudioClip> LoadClip(string path)
    {
        AudioClip clip = null;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.OGGVORBIS))
        {
            uwr.SendWebRequest();
            // wrap tasks in try/catch, otherwise it'll fail silently
            try
            {
                while (!uwr.isDone) await Task.Delay(5);

                if (uwr.isNetworkError || uwr.isHttpError) Debug.Log($"{uwr.error}");
                else
                {
                    clip = DownloadHandlerAudioClip.GetContent(uwr);
                }
            }
            catch (Exception err)
            {
                Debug.Log($"{err.Message}, {err.StackTrace}");
            }
        }

        return clip;
    }

    public void GetDataFromMidi()
    {
        notes = midiFile.GetNotes();

        foreach (var lane in lanes) lane.SetTimeStamps(notes);

        Invoke(nameof(StartSong), songDelayInSeconds);
    }
    public void StartSong()
    {
        Instance.audioSource.Play();

        Instance.audioSources[0].Play();
        //if (Instance.audioSources[1]) Instance.audioSources[1].Play();
        //if (Instance.audioSources[2]) Instance.audioSources[2].Play();

        played = 1;
    }
    public static double GetAudioSourceTime()
    {
        return (double)Instance.audioSource.timeSamples / Instance.audioSource.clip.frequency;
    }

    void Update()
    {
        if (Instance.audioSource.clip.name == "guitar" && !Instance.audioSource.isPlaying && played == 1)
        {
            ScreenManager.Instance.changeScene("EndGameScene");
            played = 0;
        }
    }
}
