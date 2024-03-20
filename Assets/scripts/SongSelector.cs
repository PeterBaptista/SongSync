using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SongSelector : MonoBehaviour
{
    public Dropdown folderDropdown;
    public Dropdown songDropdown;
    public AudioSource audioSource;
    public string path = string.Empty;
    public ScreenManager screenManager;
    public SongManager songManager;
   


    private string selectedFolder;

    void Start()
    {
        // Check if the folder exists
        
    }

    public void SavePrefs(string songFolder)
    {
        PlayerPrefs.SetString("song-folder", songFolder);
        PlayerPrefs.Save();
    }


    public void OnFolderSelectionChanged()
    {
        // Update selected folder and populate song dropdown with songs from the selected folder
        selectedFolder = folderDropdown.options[folderDropdown.value].text;
        Debug.Log(selectedFolder);
        string[] songs = Directory.GetFiles(selectedFolder, "*.mp3");
        songDropdown.ClearOptions();
        songDropdown.AddOptions(new List<string>(songs));
        ScoreManager.Instance.score = 0;
        ScoreManager.Instance.comboScore = 0;
    }

    public void OnSongSelectionChanged()
    {
        // Load and play the selected song
        string selectedSong = songDropdown.options[songDropdown.value].text;
        AudioClip audioClip = Resources.Load<AudioClip>(selectedSong.Replace("Assets/Resources/", "").Replace(".mp3", ""));
        audioSource.clip = audioClip;
        audioSource.Play();
    }
}