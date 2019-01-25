using UnityEngine;
using System;
using System.IO;

#if WINDOWS_UWP
using System.Runtime.Serialization.Json;
#else
using System.Runtime.Serialization.Formatters.Binary;
#endif

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public GameObject splashButtonPanel;

    public bool isSoundActive = true;
    public int currentScene = 0;

    void Awake()
    {
        // This ensures that the game controller persists from scene to scene instead of being destroyed and re-created for each scene.
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
        }
        else if (instance != this)
            Destroy(gameObject);

        // If the audio was muted in a previous session, mute it at the start of this session.
        if (File.Exists(Application.persistentDataPath + "/playerInfo.dat"))
        {
            FileStream file = File.Open(Application.persistentDataPath + "/playerInfo.dat", FileMode.Open);

#if WINDOWS_UWP
            DataContractJsonSerializer binaryformatter = new DataContractJsonSerializer(typeof(PlayerData));
            PlayerData data = (PlayerData)binaryformatter.ReadObject(file);
            file.Dispose();
#else
            BinaryFormatter binaryformatter = new BinaryFormatter();
            PlayerData data = (PlayerData)binaryformatter.Deserialize(file);
            file.Close();
#endif

            isSoundActive = data.isSoundActive;
            if (isSoundActive)
                AudioListener.volume = 1F;
            else
                AudioListener.volume = 0F;
        }
    }

    void Start()
    {
        splashButtonPanel = GameObject.FindGameObjectWithTag("Menu");
        splashButtonPanel.SetActive(true);
    }

    public void NewGame()
    {
        // Scene 0 is the title screen, so increment it to start the first level.
        currentScene++;
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(currentScene);
    }

    public void RetryLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(currentScene);
    }

    public void NextLevel()
    {
        currentScene++;
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(currentScene);
    }

    public void SaveAndQuit()
    {
        Save();
        Quit();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Save()
    {
        PlayerData data = new PlayerData();
        data.currentScene = currentScene;
        data.isSoundActive = isSoundActive;

#if WINDOWS_UWP
        FileStream file = new FileStream(Application.persistentDataPath + "/playerInfo.dat", FileMode.Create);
        DataContractJsonSerializer binaryformatter = new DataContractJsonSerializer(typeof(PlayerData));
        binaryformatter.WriteObject(file, data);
        file.Dispose();
#else
        FileStream file = File.Create(Application.persistentDataPath + "/playerInfo.dat");
        BinaryFormatter binaryformatter = new BinaryFormatter();
        binaryformatter.Serialize(file, data);
        file.Close();
#endif
    }

    public void Load()
    {
        if (File.Exists(Application.persistentDataPath + "/playerInfo.dat"))
        {
            FileStream file = File.Open(Application.persistentDataPath + "/playerInfo.dat", FileMode.Open);
#if WINDOWS_UWP
            DataContractJsonSerializer binaryformatter = new DataContractJsonSerializer(typeof(PlayerData));
            PlayerData data = (PlayerData)binaryformatter.ReadObject(file);
            file.Dispose();
#else
            BinaryFormatter binaryformatter = new BinaryFormatter();
            PlayerData data = (PlayerData)binaryformatter.Deserialize(file);
            file.Close();
#endif
            currentScene = data.currentScene;
            // After completing the game, the player is returned to the title screen (Scene 0) AND
            // this playerInfo.dat file will exist. Hitting "Load" here will strand you on the title
            // screen. Therefore, if the currentScene is 0, start a new game.
            if (currentScene != 0)
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(currentScene);
            else
                NewGame();
        }
        else
        {
            // If a player chooses "Load Game" instead of "New Game" on a fresh installation
            // (i.e. no playerInfo.dat has been created), this will load the first level.
            NewGame();
        }
    }
}

[Serializable]
class PlayerData
{
    public int currentScene;
    public bool isSoundActive;
}