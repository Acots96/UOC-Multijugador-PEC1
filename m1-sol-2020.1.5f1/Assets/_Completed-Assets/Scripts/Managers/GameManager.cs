using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases
        public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public GameObject m_TankPrefab;             // Reference to the prefab the players will control
        public List<TankManager> m_Tanks;           // A collection of managers for enabling and disabling different aspects of the tanks

        public GameObject PlayerInputPrefab;                     // Reference to the prefab of the PlayerInput needed for each player.
        public Cinemachine.CinemachineVirtualCamera[] cameras;   // Reference to the cameras used by players.
        public GameObject miniMapCamera;                         // Reference to the minimap camera (GO instead of cinemachine because only needs to be enabled/disabled)
        public InputActionAsset[] actions;                       // Actions for each player


        private int m_RoundNumber;                  // Which round the game is currently on
        private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends
        private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won
        private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won

        private List<TankManager> remainingTanks;           // List containing the tanks that can be used by new players
        private List<TankManager> deadTanks;                // List containing the dead players
        private int activeCameras;                          // Number of active cameras at the same time
        private bool canAddTank;                            // Bool used to allow new tanks only in game time
        public string[] schemes { get; private set; }       // Schemes from the inputaction asset for all possible players


        private void Start()
        {
            // Create the delays so they only have to be made once
            m_StartWait = new WaitForSeconds (m_StartDelay);
            m_EndWait = new WaitForSeconds (m_EndDelay);

            // Definition of all schemes
            schemes = new string[4] { "KeyboardArrowsShiftCtrl", "KeyboardWASDQE", "KeyboardNumpad", "KeyboardIJKLUO" };
            canAddTank = false;

            SpawnAllTanks();

            // Once the tanks have been created and the camera is using them as targets, start the game
            StartCoroutine (GameLoop());
        }


        
        private void SpawnAllTanks() {
            activeCameras = 0;

            // Use of PlayerPrefs to get the amount of players selected from the Menu
            int amount = PlayerPrefs.GetInt("PlayersAmount", 2);

            // Tanks ready to be used by new players
            // (2 if there are 2 players since the beginning, 1 if there are 3 players, empty if 4 players)
            remainingTanks = new List<TankManager>(m_Tanks.GetRange(amount, m_Tanks.Count - amount));

            // m_tanks will contain only the active players, remainingTanks will contain the rest
            m_Tanks.RemoveRange(amount, m_Tanks.Count - amount);

            // List to keep the dead tanks, so they can be easily reset after a round.
            deadTanks = new List<TankManager>();

            // For all the tanks...
            for (int i = 0; i < m_Tanks.Count; i++) {
                // ... create them, set their player number and references needed for control
                SpawnTank(i, schemes[i]);
                // Add the corresponging camera
                AddCamera(i);
            }

            // Add the minimap camera depending on the situaction (controlled in the method)
            AddMiniMapCamera();

            // To add new players to the game (max 4), another PlayerInput is spawned,
            // which uses a different scheme than the ones used by the players as it was
            // the 5th player, but only with one single key (Space) to spawn a new player.
            PlayerInput piSpace = PlayerInput.Instantiate(PlayerInputPrefab, playerIndex: 4, controlScheme: "Keyboard&Mouse");
            piSpace.name += " (AddPlayer)";
            InputActionsManager iamSpace = new InputActionsManager();
            iamSpace.Player.AddPlayer.performed += PlayerJoined;
            piSpace.actions = iamSpace.asset;
            piSpace.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current);
        }


        private void SpawnTank(int i, string scheme) {
            // Each player needs a PlayerInput instance with its prefab (PlayerInput and MultiplayerEventSystem components),
            // its scheme (defined in the list depending on the index) 
            // and the device, being the same for all players.
            PlayerInput pi = PlayerInput.Instantiate(PlayerInputPrefab, i, schemes[i], i, Keyboard.current);

            // Each player also needs its inputaction asset object to define all the actions.
            InputActionsManager iam = new InputActionsManager();
            pi.actions = iam.asset;
            pi.SwitchCurrentControlScheme(schemes[i], Keyboard.current);

            // Second step is to instantiate the tank itself with all the information needed.
            GameObject tank = Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation);
            m_Tanks[i].m_Instance = tank;
            m_Tanks[i].m_Instance.name += " " + i;
            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].playerInput = pi;
            m_Tanks[i].inputActionsManager = iam;
            m_Tanks[i].gameManager = this;
            m_Tanks[i].Setup();  // Actions assigned here.
            m_Tanks[i].playerInput.GetComponent<PlayerInput>().SwitchCurrentControlScheme(scheme, Keyboard.current);
        }

        
        private void AddCamera(int i) {
            // Activate the corresponding camera 
            Cinemachine.CinemachineVirtualCamera newCam = cameras[i];
            GameObject cam = newCam.transform.parent.gameObject;
            cam.SetActive(true);
            // Updates these 2 necessary attributes
            newCam.Follow = m_Tanks[i].m_Instance.transform;
            newCam.LookAt = newCam.Follow;

            activeCameras++;

            Vector2 position = Vector2.zero;
            // Horizontal size depending on the number of players (full width screen for 2 players, half width for 3 or more)
            Vector2 size = new Vector2(m_Tanks.Count == 2 ? 1 : 0.5f, 0.5f);

            if (m_Tanks.Count == 1) { // In case of a winner
                position = Vector2.zero;
                size = Vector2.one;
            } else if (m_Tanks.Count == 2) {
                position = i == 0 ? Vector2.zero : Vector2.up / 2;
            } else {
                switch (i) {
                    case 0: position = Vector2.up; break;
                    case 1: position = Vector2.zero; break;
                    case 2: position = Vector2.one; break;
                    case 3: position = Vector2.right; break;
                }
                position /= 2;
            }
            cam.GetComponent<Camera>().rect = new Rect(position, size);
        }


        // This is called from start and will run each phase of the game one after another
        private IEnumerator GameLoop()
        {
            // Start off by running the 'RoundStarting' coroutine but don't return until it's finished
            yield return StartCoroutine (RoundStarting());

            // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished
            yield return StartCoroutine (RoundPlaying());

            // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished
            yield return StartCoroutine (RoundEnding());

            // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found
            if (m_GameWinner != null)
            {
                // If there is a game winner, restart the level
                m_MessageText.text = "YOU'RE THE WINNER!!";
                yield return new WaitForSeconds(2);
                SceneManager.LoadScene("Menu");
            }
            else
            {
                // If there isn't a winner yet, restart this coroutine so the loop continues
                // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end
                StartCoroutine (GameLoop());
            }
        }


        private IEnumerator RoundStarting()
        {
            // As soon as the round starts reset the tanks and make sure they can't move
            ResetAllTanks();
            DisableTankControl();

            // Snap the camera's zoom and position to something appropriate for the reset tanks
            ///m_CameraControl.SetStartPositionAndSize();

            // Increment the round number and display text showing the players what round it is
            m_RoundNumber++;
            m_MessageText.text = "ROUND " + m_RoundNumber;

            // Wait for the specified length of time until yielding control back to the game loop
            yield return m_StartWait;
        }


        private IEnumerator RoundPlaying()
        {
            // As soon as the round begins playing let the players control the tanks
            EnableTankControl();

            // Clear the text from the screen
            m_MessageText.text = string.Empty;
            if (remainingTanks.Count > 0)
                m_MessageText.text = "Añadir jugador: 'Espacio'";

            // While there is not one tank left...
            canAddTank = true;
            while (!OneTankLeft())
            {
                // Checks if any player has died to update the cameras
                UpdatePlayersAndScreen();                
                // ... return on the next frame
                yield return null;
            }
            canAddTank = false;
            UpdatePlayersAndScreen();
        }


        private IEnumerator RoundEnding()
        {
            // Stop tanks from moving
            DisableTankControl();

            // Clear the winner from the previous round
            m_RoundWinner = null;

            // See if there is a winner now the round is over
            m_RoundWinner = GetRoundWinner();

            // If there is a winner, increment their score
            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

            // Now the winner's score has been incremented, see if someone has one the game
            m_GameWinner = GetGameWinner();

            // Get a message based on the scores and whether or not there is a game winner and display it
            m_MessageText.text = string.Empty;
            string message = EndMessage();
            m_MessageText.text = message;

            // Wait for the specified length of time until yielding control back to the game loop
            yield return m_EndWait;
        }


        // This is used to check if there is one or fewer tanks remaining and thus the round should end
        private bool OneTankLeft()
        {
            // If there are one or fewer tanks remaining return true, otherwise return false
            return m_Tanks.Count <= 1;
        }
        
        
        // This function is to find out if there is a winner of the round
        // This function is called with the assumption that 1 or fewer tanks are currently active
        private TankManager GetRoundWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                // ... and if one of them is active, it is the winner so return it
                if (m_Tanks[i].m_Instance.activeSelf)
                {
                    return m_Tanks[i];
                }
            }

            // If none of the tanks are active it is a draw so return null
            return null;
        }


        // This function is to find out if there is a winner of the game
        private TankManager GetGameWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                // ... and if one of them has enough rounds to win the game, return it
                if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                {
                    return m_Tanks[i];
                }
            }

            // If no tanks have enough rounds to win, return null
            return null;
        }


        // Returns a string message to display at the end of each round.
        private string EndMessage()
        {
            // By default when a round ends there are no winners so the default end message is a draw
            string message = "DRAW!";

            // If there is a winner then change the message to reflect that
            if (m_RoundWinner != null)
            {
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";
            }

            // Add some line breaks after the initial message
            message += "\n\n\n\n";

            // Go through all the tanks (alive and dead) and adds each of their scores to the message
            for (int i = 0; i < m_Tanks.Count; i++) {
                message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
            }
            for (int i = 0; i < deadTanks.Count; i++) {
                message += deadTanks[i].m_ColoredPlayerText + ": " + deadTanks[i].m_Wins + " WINS\n";
            }

            // If there is a game winner, change the entire message to reflect that
            if (m_GameWinner != null)
            {
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";
            }

            return message;
        }


        // This function is used to turn all the tanks back on and reset their positions and properties
        private void ResetAllTanks()
        {
            activeCameras = 0;

            // All dead tanks back to live
            m_Tanks.AddRange(deadTanks);
            deadTanks.Clear();

            for (int i = 0; i < m_Tanks.Count; i++) {
                m_Tanks[i].Reset();
                AddCamera(i);
            }

            if (m_Tanks.Count == 4)
                m_MessageText.text = string.Empty;
            AddMiniMapCamera();
        }


        private void EnableTankControl()
        {
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                m_Tanks[i].EnableControl();
            }
        }


        private void DisableTankControl()
        {
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                m_Tanks[i].DisableControl();
            }
        }


        // Function used to add new tank to the game (updates the lists and the cameras on screen)
        private void AddNewTank() {
            // Cannot add more if max players reached
            if (remainingTanks.Count == 0)
                return;

            // Get the next tank stored and spawns it
            m_Tanks.Add(remainingTanks[0]);
            remainingTanks.RemoveAt(0);
            SpawnTank(m_Tanks.Count - 1, schemes[m_Tanks.Count - 1]);

            // Updates all the cameras according to the new ammount of tanks
            activeCameras = 0;
            for (int i = 0; i < m_Tanks.Count; i++)
                AddCamera(i);
            AddMiniMapCamera();

            m_MessageText.text = string.Empty;
            if (remainingTanks.Count > 0)
                m_MessageText.text = "Añadir jugador: 'Espacio'";
        }


        // Function used to add the minimap camera if there are 3 players. Removes it otherwise
        private void AddMiniMapCamera() {
            if (m_Tanks.Count == 3) {
                miniMapCamera.SetActive(true);
                miniMapCamera.GetComponent<Camera>().rect = new Rect(Vector2.right / 2, Vector2.one / 2);
            } else {
                miniMapCamera.SetActive(false);
            }
        }


        // Function used to check if any player is dead, to remove its camera
        private void UpdatePlayersAndScreen() {
            // Find if anyone is dead and updates the lists.
            foreach (TankManager tank in m_Tanks)
                if (!tank.m_Instance.activeSelf) {
                    m_Tanks.Remove(tank);
                    deadTanks.Add(tank);
                    break;
                }

            // No need to update the cameras if there is the same amount of players than the cameras
            if (activeCameras == m_Tanks.Count)
                return;
            activeCameras = 0;

            // Add cameras to all the tanks alive
            for (int i = 0; i < m_Tanks.Count; i++)
                AddCamera(i);
            // Disable the cameras of the dead tanks
            for (int i = m_Tanks.Count; i < cameras.Length; i++)
                cameras[i].transform.parent.gameObject.SetActive(false);
            AddMiniMapCamera();

            m_MessageText.text = string.Empty;
            if (remainingTanks.Count > 0)
                m_MessageText.text = "Añadir jugador: 'Espacio'";
        }



        // Method called when Space pressed
        public void PlayerJoined(InputAction.CallbackContext context) {
            if (canAddTank)
                AddNewTank();
        }
    }
}