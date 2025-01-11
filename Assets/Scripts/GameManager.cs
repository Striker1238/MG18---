using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

// �������� ����� ����� ���������������� ���� ���� � ����� ��� ������ ����, � �����
// �������������� ��������� ������ UI ���������.
// �� ������ UI � ������ ������ ���������, �� ����� ������ ��������� ������ �������� UI �� ��������� �����(��� ����� SOLID)
public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    public int nodeCount = 10; // ���������� �����
    [Min(1)]public int lineOneNode = 3; // ���������� ����� �� ���� ����, �� ����������� ���������
    public Vector2 playAreaSize = new Vector2(10, 10); // ������ ������� ����

    [Header("Prefabs")]
    public GameObject nodePrefab; // ������ ����
    public LineRenderer linePrefab; // ������ �����

    [Header("Material Rope")]
    public Material greenLine; // �������� � ������� ������
    public Material redLine; // �������� � ������� ������

    [Header("UI Elements")]
    public Button skipButton;
    public TextMeshProUGUI scoreText;
    public GameObject levelComplatePanel;

    [Header("AudioSource")]
    public AudioSource audio;


    private List<GameObject> nodes = new List<GameObject>();
    private List<LineRenderer> lines = new List<LineRenderer>();
    private Dictionary<GameObject, List<LineRenderer>> nodeConnections = new Dictionary<GameObject, List<LineRenderer>>();

    private int score = 0;
    private bool levelComplete = false; // ����������� ���������� ������

    void Start()
    {
        // ���������� ���� �� ������� ����
        GenerateNodes();
        // ���������� ����� ����� ������
        GenerateLines();
        
        UpdateLineColors();
    }

    // ���������� ���� �� �������� ����, � ��������� ��������
    void GenerateNodes()
    {
        for (int i = 0; i < nodeCount; i++)
        {
            // �������� ���������� ��� ����
            Vector2 randomPosition = new Vector2(
                Random.Range(-playAreaSize.x / 2, playAreaSize.x / 2),
                Random.Range(-playAreaSize.y / 2, playAreaSize.y / 2));

            GameObject node = Instantiate(nodePrefab, randomPosition, Quaternion.identity);
            nodes.Add(node);
            // ������� � ����� ������� ������ ������ ����� � ����
            nodeConnections[node] = new List<LineRenderer>();
        }
    }

    // ������� ���������� ����� ������ � ������������ ��������
    void GenerateLines()
    {
        foreach (GameObject node in nodes)
        {
            int attempts = 0;
            while (nodeConnections[node].Count < lineOneNode && attempts < 100)
            {
                // �������� ��������� ���� ��� ���������� � ������� 
                GameObject targetNode = GetRandomNode(node);

                // ��������� ��� � ��� ���� ���� � � ��� ��� ����� ���� ��� ����������
                if (targetNode != null && !AreNodesConnected(node, targetNode))
                {
                    // ������� ���������� � ��������� �������
                    LineRenderer line = Instantiate(linePrefab);
                    lines.Add(line);
                    UpdateLinePosition(line, node.transform.position, targetNode.transform.position);

                    // ��������� � ������� � ����� ����� ����� 
                    nodeConnections[node].Add(line);
                    nodeConnections[targetNode].Add(line);
                }
                attempts++;
            }
        }
    }

    // �������� ��������� ����, ������� ������������� ��������
    GameObject GetRandomNode(GameObject excludeNode)
    {
        List<GameObject> potentialNodes = new List<GameObject>();
        foreach (GameObject node in nodes)
        {
            // ���������, ��� ���� �� ����� ������ ������������� ���������� �����
            if (nodeConnections[node].Count < lineOneNode && node != excludeNode)
                potentialNodes.Add(node);
        }
        // ���������� ��������� ���� �� ������, ���� ������ �� ������
        return potentialNodes.Count > 0 ? potentialNodes[Random.Range(0, potentialNodes.Count)] : null;
    }

    // ��������, ��� ���� � � ���� � ��� �� ���������
    bool AreNodesConnected(GameObject nodeA, GameObject nodeB)
    {
        foreach (LineRenderer line in nodeConnections[nodeA])
        {
            if (nodeConnections[nodeB].Contains(line))
            {
                return true;
            }
        }
        return false;
    }

    // ��������� �����, ���������� �������� �������
    public void UpdateLines(GameObject moveNode)
    {
        foreach (var node in nodes)
        {
            foreach (var line in nodeConnections[node])
            {
                var otherNode = GetOtherNode(line, node);
                if (otherNode != null)
                {
                    UpdateLinePosition(line, node.transform.position, otherNode.transform.position);
                    audio.mute = false;

                    if(node == moveNode)
                        line.material = redLine;
                }
            }
        }
    }

    // �������� ������ ���� �� �����
    GameObject GetOtherNode(LineRenderer line, GameObject currentNode)
    {
        GameObject? endNode = nodes.Find(node => nodeConnections[node].Contains(line));

        if (endNode != null) return endNode;
        else return null;
    }

    // ��������� ������� �����
    void UpdateLinePosition(LineRenderer line, Vector3 start, Vector3 end)
    {
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    // ��������� ���� ����� ��� ������������ �������
    public void UpdateLineColors()
    {
        bool allLinesClear = true;
        foreach (LineRenderer line in lines)
        {
            bool intersects = false;
            foreach (LineRenderer otherLine in lines)
            {
                if (line != otherLine && LinesIntersect(line, otherLine))
                {
                    intersects = true;
                    break;
                }
            }

            line.material = intersects ? redLine : greenLine;//line.endColor = intersects ? Color.red : Color.green;
            if (intersects) allLinesClear = false;
        }
        // ���� ��� ����� ���������� ��������� ���������� ����� ����������� �������
        if (allLinesClear && !levelComplete) 
        {
            CompleteLevel();
        }
    }
    // ���������, ������������ �� �����
    bool LinesIntersect(LineRenderer line1, LineRenderer line2)
    {
        Vector2 p1 = line1.GetPosition(0);
        Vector2 p2 = line1.GetPosition(1);
        Vector2 q1 = line2.GetPosition(0);
        Vector2 q2 = line2.GetPosition(1);

        return DoLinesIntersect(p1, p2, q1, q2);
    }

    bool DoLinesIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {

        // �������� ������������ 
        float d = (q2.y - q1.y) * (p2.x - p1.x) - (q2.x - q1.x) * (p2.y - p1.y);
        // ����� �� ������������
        if (d == 0) return false;

        //���� ����� �����������
        float u = ((q2.x - q1.x) * (p1.y - q1.y) - (q2.y - q1.y) * (p1.x - q1.x)) / d;
        float v = ((p2.x - p1.x) * (p1.y - q1.y) - (p2.y - p1.y) * (p1.x - q1.x)) / d;

        // ���� ����� �� ������ � ���������� (0;1) �� ����� �� ������������ + ��������� �����������
        return (u - 0.01f) > 0 && (u + 0.01f) < 1 && (v - 0.01f) > 0 && (v + 0.01f) < 1;
    }

    Task CompleteLevel()
    {
        levelComplete = true;
        Debug.Log("Level Complete!");

        // �������� ������, ����� ��������
        levelComplatePanel.SetActive(true);
        // �������� ��� ������� ��������, ����� �� �� ����, ����� ���� �� ����������� ����� DoTween ������� ��������
        levelComplatePanel.GetComponent<Animator>().Play("LevelComplateAnimator");


        score += 350;
        scoreText.text = "Score: " + score;
        // �������������: ��������, �������
        return Task.CompletedTask;
    }

    public async void SkipLevel()
    {
        Debug.Log("Level Skipped");

        await CompleteLevel();
        await Task.Delay(3000);

        FindObjectOfType<SceneController>().LoadScene(0);
    }

    // ��������� �������� ����, ��� ������� ��������� � ���������
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(playAreaSize.x, playAreaSize.y, 0));
    }

}
