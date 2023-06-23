using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public enum GameMode
{
    Idle,
    Build,
    Combat,
    GameOver,
    LvlComplete,
}

public class GameManager : MonoBehaviour
{
    [Header("Difficulty Modifiers")]
    [Tooltip("(Difficulty) The amount of times that the game cycles between build mode and combat mode.")] [SerializeField] [Range(1, 6)] public int amountOfCycles;
    [Tooltip("(Difficulty) The amount of enemies to spawn per generator.")] [SerializeField] [Range(1, 100)] public int enemiesPerGenerator;
    [Tooltip("(Difficulty) The amount of enemies that are added to the enemiesPerGenerator, per each cycled.")] [SerializeField] [Range(1, 40)] public int enemiesIncreasePerCycle;
    [Tooltip("(Difficulty) The amount of enemies that spawn per generator in wave one.")] [SerializeField] [Range(1, 25)] public int waveOneEnemies = 10;

    [Tooltip("This is the amount of time that a player will have in either of the specified modes.")] [SerializeField] [Range(10, 49)] public int timeInBuildMode = 30, timeInCombatMode = 30, timeInWaveOneBuild =30;

    [Header("Technical")]
    [Tooltip("This is the current GameMode that majorly effects how the game acts. Idle = 0, Build = 1, Combat = 2")]public GameMode currentMode = GameMode.Build;

    [SerializeField] GameObject CombatCanvas, BuildCanvas, MainCanvas, GameOverCanvas, MissionSucessCanvas;
    [Tooltip("This will be turned off when the player is in Build Mode!")][SerializeField] GameObject WeaponsParent;
    [Tooltip("This is the text shown for teh game time that is remaining.")] [SerializeField] TextMeshProUGUI GameTimerText;
    [Tooltip("This is the text shown for the current game cycle.")] [SerializeField] TextMeshProUGUI GameCycleText;
    [Tooltip("This is the text shown for the current amount of enemies alive.")] [SerializeField] TextMeshProUGUI EnemiesAliveText;

    private float timerTime = 0; /// The Displayed timer time on the GUI. Not for technical functionality, only for the GUI functionality.
    private int currentCycle = 1;

    private Color gameTimeDefaultColor; // Default color of the game timer, used when making time turn red for the last 9 seconds of a cycle.
    public static int enemiesAlive;
    public int _enemiesAlive;

    public static bool hasWon;

    [Tooltip("A reference for the health object of each of the generators in the level.")] [SerializeField] Health[] GeneratorsInLevel;

    [Tooltip("The music player for the game.")] [SerializeField] GameObject GameMusicPlayer;

    private void Start()
    {
        /// Initialize the game, Restart static values
        InvokeRepeating(nameof(UpdateGenerators), 0, 3);
        InvokeRepeating(nameof(TickGameTimer), 0, .1f);
        StartCoroutine(GameCycleSequence());
        SwitchGamemode(GameMode.Build);

        enemiesAlive = 0;
        hasWon = false;

        gameTimeDefaultColor = GameTimerText.color;

        GameOverCanvas.SetActive(false);
        MissionSucessCanvas.SetActive(false);
        MainCanvas.SetActive(true);

        Time.timeScale = 1.0f;
        AudioListener.pause = false;
    }



    /// <summary>
    /// Update timer time every second.
    /// </summary>
    private void TickGameTimer()
    {
        if (timerTime > 0) timerTime-=0.1f;

        // Format the text
        if (timerTime > 9) { GameTimerText.color = gameTimeDefaultColor; GameTimerText.text = timerTime.ToString("##.#"); }
        else { GameTimerText.color = Color.red; GameTimerText.text = timerTime.ToString("#.##"); }
    }

    private void UpdateGenerators()
    {
        /// Handle health of generators 
        List<bool> generatorIsAlive = new List<bool>();
        foreach (Health generatorHealth in GeneratorsInLevel) { if (generatorHealth.currentHealth > 0) generatorIsAlive.Add(true); else generatorIsAlive.Add(false); }


        bool hasLost = true;

        for (int i = 0; i < generatorIsAlive.Count; ++i)
        {
            if (generatorIsAlive[i])
            {
                hasLost = false;
            }
        }

        if (hasLost)
        {
            PlayerManager.generatorsDestroyed = true;
            //GameMusicPlayer.SetActive(false); 
            Debug.Log("All generators destroyed. Player has lost.");
        }
      
    
    }
    private IEnumerator GameCycleSequence()
    {
        for (int i = 1; i < amountOfCycles+1; i++)
        {
            Debug.Log($"<color=cyan>[Game Manager]</color> Begin Cycle {i}/{amountOfCycles}.");
            GameCycleText.text = $"{i}/{amountOfCycles}";
            enemiesAlive = 0;

            /// Starts in build mode
            SwitchGamemode(GameMode.Build);
            if (i > 1)
            {
                timerTime = timeInBuildMode;
                yield return new WaitForSeconds(timeInBuildMode);
            }
            else // Distinction between wave one difficulty and all of the other difficulties
            {
                timerTime = timeInWaveOneBuild;
                yield return new WaitForSeconds(timeInWaveOneBuild);
            }
            /// Provide 5 seconds between cycles. Do not show on the game timer. This is invisible time.
            timerTime = 0;
            yield return new WaitForSeconds(2);
            /// Switch to combat mode
            SwitchGamemode(GameMode.Combat);
            /// Initiate the combat mode
            foreach (EnemySpawner spawner in FindObjectsOfType<EnemySpawner>())
            {
                if (i > 1)
                {
                    spawner.enemiesToSpawn = enemiesPerGenerator;
                    enemiesAlive += enemiesPerGenerator;
                }
                else // Distinction between wave one difficulty and all of the other difficulties
                {
                    spawner.enemiesToSpawn = waveOneEnemies;
                    enemiesAlive += waveOneEnemies;
                }

                spawner.StartCoroutine(spawner.SpawnWave());
            }
            /// Wait until all enemies are dead, not for the timer, to set the round to a win state.
            timerTime = 0;
            while (enemiesAlive > 0) yield return null;

            /// Provide 5 seconds between cycles. Do not show on the game timer. This is invisible time.
            timerTime = 0;
            yield return new WaitForSeconds(5);
            /// Cycle has been won
            currentCycle++;
            if (i > 1)
            {
                enemiesPerGenerator += enemiesIncreasePerCycle; // Increase game difficulty if it isn't the first wave.
            }
            /// Game Has been won
            if (i == amountOfCycles)
            {
                hasWon = true;

                LevelComplete();
                Debug.Log("<b>[Game Manager]</b> <color=green>Game Won! (Show UI Prompt Now)</color>");
            }
        }
    }


    /// <summary>
    /// The only audio player that the FX come out of.
    /// </summary>
    public AudioSource CoreFXPlayer;
    
    /// <summary>
    /// The only audio player that the Music should come out of.
    /// </summary>
    public AudioSource CoreMusicPlayer;

    public static AudioSource GetCorePlayer() { return FindObjectOfType<GameManager>().CoreFXPlayer; }

[SerializeField] public GameObject BuildMenu, DeleteMenu;


    private void Update()
    {
        _enemiesAlive = enemiesAlive;
        EnemiesAliveText.text = enemiesAlive.ToString();
        switch (currentMode)
        {
            case GameMode.Build:
                CombatCanvas.SetActive(false);
                BuildCanvas.SetActive(true);
                WeaponsParent.SetActive(false);

                foreach (BuildNode node in FindObjectsOfType<BuildNode>())
                {
                    node.Enable(); // Show all node mesh renderes
                } 


                break;

            case GameMode.Combat:
                CombatCanvas.SetActive(true);
                BuildCanvas.SetActive(false);
                WeaponsParent.SetActive(true);
                foreach (BuildNode node in FindObjectsOfType<BuildNode>()) node.Disable(); // Hide all node mesh renderers

                break;

            case GameMode.GameOver:
                GameMusicPlayer.SetActive(false);

                MainCanvas.SetActive(false);
                GameOverCanvas.SetActive(true);
                Cursor.visible = true;
                break;

            case GameMode.LvlComplete:
                GameMusicPlayer.SetActive(false);

                MainCanvas.SetActive(false);
                MissionSucessCanvas.SetActive(true);
                Cursor.visible = true;
                break;

            default: 
                CombatCanvas.SetActive(false);
                BuildCanvas.SetActive(false);
                WeaponsParent.SetActive(false);


                foreach (BuildNode node in FindObjectsOfType<BuildNode>()) node.Disable(); // Hide all node mesh renderers

                break;
        }



        // Surge Breaker (1.5p)
        // This will stop all coroutines, and pause the game. This means, animations will need to be animators and mustn't rely on TimeScale if an animation is to be seen within Build Mode. 
        // This is primarily used for pausing the spawning of waves while in build mode.
        // This method may be deprecated in the future due to all of the problems that may come from pausing time itself while in build mode. Try not to take this off of your radar.

            // if (currentMode == GameMode.Build) Time.timeScale = 0;
            // else Time.timeScale = 1; // Normalize Time Scale. This may need more TLC if there is a pause menu that is implemented.
    }



    public void SwitchGamemode(GameMode switchTo) // This manually alternates the gamemode
    {
        currentMode = switchTo;

        Debug.Log($"Switched gamemode to <color=\"yellow\">{currentMode}</color>.");
    }


    public void SwitchGamemode() // This automatically alternates the gamemode when you press tab
    {
        if (currentMode == GameMode.Build)
        {
            currentMode = GameMode.Combat;
        }
        else if (currentMode == GameMode.Combat)
        {
            currentMode = GameMode.Build;
        }
        else if (currentMode.GetHashCode() == 0)
        {
            currentMode++;
        }

        Debug.Log($"Switched gamemode to <color=\"yellow\">{currentMode}</color>.");
    }

    public void  GameOver() // Called by the PlayerManager when a lose condition is met
    {
        SwitchGamemode(GameMode.GameOver);
    }

    public void LevelComplete()
    {
        SwitchGamemode(GameMode.LvlComplete);
    }

    public void LoadLevel(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
    }

}
