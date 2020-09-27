using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour {

    public void SelectPlayers(int amount) {
        PlayerPrefs.SetInt("PlayersAmount", amount);
        SceneManager.LoadScene("_Complete-Game");
    }

}
