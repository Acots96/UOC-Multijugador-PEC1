using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour {

    [SerializeField]
    private GameObject[] HiddenTexts;
    [SerializeField]
    private GameObject StartingText;

    private int firstSelected;
    private bool gameStarted;


    public void Awake() {
        firstSelected = 0;
        gameStarted = false;
        HiddenTexts[firstSelected].SetActive(true);
    }


    /**
     * Selects a button and enables the corresponding text,
     * so the user will know how many players will be playing
     * and their corresponding set of keys.
     */
    public void OnNavigate(int button) {
        if (gameStarted)
            return;
        firstSelected = button;
        HiddenTexts[firstSelected].SetActive(true);
        for (int i = firstSelected + 1; i < HiddenTexts.Length; i++)
            HiddenTexts[i].SetActive(false);
    }


    /**
     * When the selected button is pressed, the game starts with
     * the amount of players selected.
     */
    public void SelectPlayers(int amount) {
        gameStarted = true;
        StartingText.SetActive(true);
        StartCoroutine(StartGame(amount));
    }

    private IEnumerator StartGame(int amount) {
        yield return new WaitForSeconds(1f);
        PlayerPrefs.SetInt("PlayersAmount", amount);
        SceneManager.LoadScene("_Complete-Game");
    }

}
