using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        EnableText(0, true);
        EnableText(1, false);
        EnableText(2, false);
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
        // Activates the text corresponding to the selected button
        // and hids the next ones.
        EnableText(firstSelected, true);
        for (int i = firstSelected + 1; i < HiddenTexts.Length; i++)
            EnableText(i, false);
    }

    /**
     * Method used to increase/decrease the alpha component
     * of a text's color.
     */
    private void EnableText(int idx, bool enable) {
        Text t = HiddenTexts[idx].GetComponent<Text>();
        Color c = t.color;
        c.a = enable ? 1 : 0.3f;
        t.color = c;
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
