using UnityEngine;
using UnityEngine.UI;

public class PlayerContoller : MonoBehaviour
{
    public float initialVelocity;
    public AudioClip endSound;
    public AudioClip engineSound;
    public AudioClip explosionSound;
    public AudioClip outOfBoundsSound;

    private bool initialBurn = false;
    private bool launched = false;
    private bool stopped = false;
    private float planetaryAttraction;
    private Vector3 mouseWorldSpace;
    private bool isPaused = false;

    // Cached references (for performance)
    private Rigidbody2D player;
    private GameObject[] planets;
    private Animator animator;
    private AudioSource source;
    private SpriteRenderer spriteRenderer;
    private GameObject UImenu;
    private Text frameText;
    private Button[] levelButtons;
    private Text levelButtonText;
    private GameObject tutorialPanel;
    
    // Use this for initialization
    void Start()
    {
        player = GetComponent<Rigidbody2D>();
        planets = GameObject.FindGameObjectsWithTag("Obstacle");
        animator = GetComponent<Animator>();
        source = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        UImenu = GameObject.FindGameObjectWithTag("Menu");
        UImenu.SetActive(false);

        // The "Toggle Sound" button is unique because it is the only menu option that returns
        // the player to the same scene. (All others: retry, load the next scene or quit the game.)
        // Setting the text and listeners in the Pause menu section of Update() causes the toggle
        // to be run multiple times per button click.  Therefore I'm making the Pause Menu text and
        // listeners the default options here in Start().
        frameText = UImenu.GetComponentInChildren<Text>();
        frameText.text = "Game options";
        levelButtons = UImenu.GetComponentsInChildren<Button>();
        levelButtonText = levelButtons[0].GetComponentInChildren<Text>();
        levelButtonText.text = "Toggle sound";
        levelButtons[0].onClick.AddListener(ToggleSound);
        levelButtons[1].onClick.AddListener(GameController.instance.SaveAndQuit);

        tutorialPanel = GameObject.FindGameObjectWithTag("Tutorial");
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_ANDROID
        if ( Input.touchCount > 0 )
        {
            Touch myTouch = Input.GetTouch(0);

            if(tutorialPanel.activeSelf && myTouch.phase == TouchPhase.Ended)
                tutorialPanel.SetActive(false);
        
            if (!launched)
            {
                mouseWorldSpace = Camera.main.ScreenToWorldPoint(myTouch.position);
                mouseWorldSpace.z = -1000.0f;
                transform.LookAt(mouseWorldSpace, Vector3.forward);
                transform.eulerAngles = new Vector3(0, 0, -transform.eulerAngles.z);
                
                if ( myTouch.phase == TouchPhase.Ended && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    animator.SetTrigger("Launched");
                    source.PlayOneShot(engineSound, 1F);
                    launched = true;
                }
            }
        }
#else
        // The tutorial panel (if it exists in this scene) is dismissed with a click.
        if (tutorialPanel.activeSelf && Input.GetButtonDown("Fire1"))
            tutorialPanel.SetActive(false);

        // The player rotates to match the cursor position until the ship is launched.
        if (!launched)
        {
            // Convert the mouse location from screen space to world space.
            mouseWorldSpace = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // ScreenToWorldPoint() automatically converts the z coordinate from 0.0 to -10.0.
            // At -10, the angle becomes inaccurate as the player moves across the screen. 
            // Setting it to 0, makes the player invisible. 
            // Smaller z's seem to increase accuracy over distance. (-1000 is simply an "accurate feeling" magic number.)
            mouseWorldSpace.z = -1000.0f;
            transform.LookAt(mouseWorldSpace, Vector3.forward);
            transform.eulerAngles = new Vector3(0, 0, -transform.eulerAngles.z);

            // The IsPointerOverGameObject() allows the Pause Menu buttons to be clicked without launching the ship.
            if ( Input.GetButtonDown("Fire1") && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                animator.SetTrigger("Launched");
                source.PlayOneShot(engineSound, 1F);
                launched = true;
            }
        }
#endif
        
        // The pause menu is called up by ESC, Menu button, et al.
        if ( Input.GetKeyDown(KeyCode.Escape) )
        {
            if (!isPaused)
            {
                isPaused = true;
                Time.timeScale = 0;
                UImenu.SetActive(true);
            }
            else
            {
                // If the user decides not to select a pause menu option, dismiss the menu and resume the game.
                isPaused = false;
                Time.timeScale = 1;
                UImenu.SetActive(false);
            }
        }
    }

    void FixedUpdate()
    {
        if (launched)
        {
            // When the player launches, we give him a quick boost before letting gravity take over.
            if (!initialBurn)
            {
                player.AddRelativeForce(new Vector2(0, initialVelocity));
                initialBurn = true;
            }

            if (!stopped)
            {
                foreach (GameObject planet in planets)
                {
                    // The force of gravity is defined as: Force = (Gravitational constant * mass1 * mass2)/distance^2.
                    // Because player's mass is negligible compared to the planet, we can lump the Gravitational constant and the
                    // planetary mass into a single "Planetary Attraction" variable (technically the Standard Gravitational Parameter).
                    Vector2 distance = planet.transform.position - transform.position;
                    float distanceSquared = distance.sqrMagnitude;
                    planetaryAttraction = planet.GetComponent<PlanetScript>().planetaryAttraction;

                    // Make sure I'm not dividing by zero.
                    if (distanceSquared != 0)
                    {
                        // "distance.normalized" gives a direction and converts our result to a Vector.
                        player.AddForce(planetaryAttraction * distance.normalized / distanceSquared);
                    }
                }

                // The player snaps to "up" when the mouse button is clicked and then snaps back to the aimed angle.
                // My guess is that this is the velocity vector briefly being zero between Update() and FixedUpdate().
                // This "if" statement prevents this "blip" to zero.
                if (player.velocity.magnitude > 0)
                {
                    // This faces the player in the direction that he is moving.
                    transform.up = player.velocity;
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
	{
        // Stop physics from acting on the player.
        player.Sleep();
        stopped = true;

        // Bring up the menu and disconnect the code from the top menu button.
        UImenu.SetActive(true);
        levelButtons[0].onClick.RemoveListener(ToggleSound);

        if (other.gameObject.CompareTag("Background"))
		{
            // Play the "lost" audio clip.
            source.PlayOneShot(outOfBoundsSound, 1F);

            // Swap out the Pause menu title.
            frameText.text = "Ship Lost!";
            // Swap out the top menu button text and code.
            levelButtonText.text = "Retry level";
            levelButtons[0].onClick.AddListener(GameController.instance.RetryLevel);
        }
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            // Play the "destroyed" animation and audio clip.
            animator.SetTrigger("Destroyed");
            source.PlayOneShot(explosionSound, 1F);

            // Swap out the Pause menu title.
            frameText.text = "Ship Lost!";
            // Swap out the top menu button text and code.
            levelButtonText.text = "Retry level";
            levelButtons[0].onClick.AddListener(GameController.instance.RetryLevel);
        }
        else if (other.gameObject.CompareTag("Finish"))
        {
            // Turn off the ship and play the docking animation. (The animation is on the space station object.)
            spriteRenderer.enabled = false;
            other.gameObject.GetComponent<Animator>().SetTrigger("Docked");
            source.PlayOneShot( endSound, 1F );

            // Swap out the Pause menu title.
            frameText.text = "Success!";
            // Swap out the top menu button text and code.
            levelButtonText.text = "Next Level";
            levelButtons[0].onClick.AddListener(GameController.instance.NextLevel);
        }
    }

    void ToggleSound()
    {
        if (GameController.instance.isSoundActive )
        {
            Debug.Log("Turning sound off.");
            AudioListener.volume = 0F;
            GameController.instance.isSoundActive = false;
        }
        else
        {
            Debug.Log("Turning sound on.");
            AudioListener.volume = 1.0F;
            GameController.instance.isSoundActive = true;
        }

        // When the "Toggle Sound" button is clicked, dismiss the menu and resume the game.
        isPaused = false;
        Time.timeScale = 1;
        UImenu.SetActive(false);
    }
}
