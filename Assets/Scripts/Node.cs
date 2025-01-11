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
        // ��������� �������� ����� �������� ���� � ��������
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        offset = transform.position - mousePosition;
    }
    // ������ ���������� ��� ����
    private void OnMouseDrag()
    {
        // ��������� ������� ���� �� ������ ������� �������
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        transform.position = mousePosition + offset;

        
        var gameManager = FindObjectOfType<GameManager>();
        // �������� ���� �� ���������� ��������� �����
        gameManager.UpdateLines(gameObject);

        if (gameManager.audio.isPlaying == false)
            gameManager.audio.Play();
    }
    private void OnMouseUp()
    {
        var gameManager = FindObjectOfType<GameManager>();
        gameManager.audio.Stop();
        // ��������� ���� �����, ����� ��������� ������� ����
        gameManager.UpdateLineColors();
    }

    private void SelectNode(bool state)
    {
        nodeOutline.SetActive(state);
    }
}
