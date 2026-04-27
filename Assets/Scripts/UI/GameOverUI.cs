using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI message;

    void Update()
    {
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER)
        {
            panel.SetActive(true);
            message.text = GameManager.Instance.playerWon ? "You Win!" : "You Lost";
        }
        else
        {
            panel.SetActive(false);
        }
    }

    public void ReturnToStart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
