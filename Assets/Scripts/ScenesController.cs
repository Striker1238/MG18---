
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void LoadScene(int numberScene)
    {
        SceneManager.LoadScene(numberScene);
    }
}
