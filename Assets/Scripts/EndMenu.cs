using UnityEngine;
using UnityEngine.UI;

// This sets up the buttons on the Game Completion screen.

public class EndMenu : MonoBehaviour
{
    private GameObject creditsPanel;
    private GameObject UImenu;
    private Button[] buttons;

    void Start()
    {
        creditsPanel = GameObject.FindGameObjectWithTag("Tutorial");
        creditsPanel.SetActive(false);

        UImenu = GameObject.FindGameObjectWithTag("Menu");
        buttons = UImenu.GetComponentsInChildren<Button>();
        buttons[0].onClick.AddListener(MainMenu);
        buttons[1].onClick.AddListener(ShowCredits);
        buttons[2].onClick.AddListener(EndQuit);
    }

    void Update()
    {
        // The Credits panel is dismissed with a click.
        if (creditsPanel.activeSelf && Input.GetButtonDown("Fire1"))
            creditsPanel.SetActive(false);
    }

    public void MainMenu()
    {
        // Turn the Main Menu buttons panel back on and then load the Splash Screen.
        GameController.instance.splashButtonPanel.SetActive(true);
        GameController.instance.currentScene = 0;
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(GameController.instance.currentScene);
    }

    public void ShowCredits()
    {
        creditsPanel.SetActive(true);
    }

    public void EndQuit()
    {
        // Reset the current level to the splash screen, so that you cannot load into the Completion screen.
        GameController.instance.currentScene = 0;
        GameController.instance.SaveAndQuit();
    }
}
