using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaTrigger : MonoBehaviour
{
    public string areaName;
    public string cenaDoMinigame = "Minigame_Template";

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() == null)
        {
            return;
        }

        Debug.Log("Entrou na área: " + areaName);
        SceneManager.LoadScene(cenaDoMinigame);
    }
}
