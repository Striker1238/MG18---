using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Node : MonoBehaviour
{
    public GameObject nodeOutline;
    private Vector3 offset;
    [SerializeField]private AudioSource audio;
    private void OnMouseEnter()
    {
        SelectNode(true);
    }
    private void OnMouseExit()
    {
        SelectNode(false);
    }
    private void OnMouseDown()
    {
        audio.Play();
        // Сохраняем смещение между позицией узла и курсором
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        offset = transform.position - mousePosition;
    }
    // Плавно перемещаем наш узел
    private void OnMouseDrag()
    {
        // Обновляем позицию узла на основе позиции курсора
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        transform.position = mousePosition + offset;

        
        var gameManager = FindObjectOfType<GameManager>();
        // Сообщаем игре об обновлении положения линий
        gameManager.UpdateLines(gameObject);

        if (gameManager.audio.isPlaying == false)
            gameManager.audio.Play();
    }
    private void OnMouseUp()
    {
        var gameManager = FindObjectOfType<GameManager>();
        gameManager.audio.Stop();
        // Обновляем цвет линий, когда перестаем двигать узел
        gameManager.UpdateLineColors();
    }

    private void SelectNode(bool state)
    {
        nodeOutline.SetActive(state);
    }
}
