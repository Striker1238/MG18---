using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

// Основной класс будет инициализировать наши узлы и линии при старте игры, а также
// контролировать различную логику UI элементов.
// Тк логика UI в данный момент маленькая, не видел смысла разбивать логику контроля UI на отдельный класс(тот самый SOLID)
public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    public int nodeCount = 10; // Количество узлов
    [Min(1)]public int lineOneNode = 3; // Количество линий на одну ноду, за исключением последней
    public Vector2 playAreaSize = new Vector2(10, 10); // Размер игровой зоны

    [Header("Prefabs")]
    public GameObject nodePrefab; // Префаб узла
    public LineRenderer linePrefab; // Префаб линии

    [Header("Material Rope")]
    public Material greenLine; // Материал с зеленой линией
    public Material redLine; // Материал с красной линией

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
    private bool levelComplete = false; // Отслеживает завершение уровня

    void Start()
    {
        // Генерируем узлы на игровом поле
        GenerateNodes();
        // Генерируем линии между узлами
        GenerateLines();
        
        UpdateLineColors();
    }

    // Генерируем ноды по игровому полю, в рандомных позициях
    void GenerateNodes()
    {
        for (int i = 0; i < nodeCount; i++)
        {
            // Рандомим координаты для узла
            Vector2 randomPosition = new Vector2(
                Random.Range(-playAreaSize.x / 2, playAreaSize.x / 2),
                Random.Range(-playAreaSize.y / 2, playAreaSize.y / 2));

            GameObject node = Instantiate(nodePrefab, randomPosition, Quaternion.identity);
            nodes.Add(node);
            // Создаем в нашем словаре пустой список линий у узла
            nodeConnections[node] = new List<LineRenderer>();
        }
    }

    // Создаем соединения между узлами с определенным условием
    void GenerateLines()
    {
        foreach (GameObject node in nodes)
        {
            int attempts = 0;
            while (nodeConnections[node].Count < lineOneNode && attempts < 100)
            {
                // Получаем случайный узел для соединения с текущей 
                GameObject targetNode = GetRandomNode(node);

                // Проверяем что у нас есть узел и у нас нет между ними уже соединений
                if (targetNode != null && !AreNodesConnected(node, targetNode))
                {
                    // Создаем соединение и обновляем позицию
                    LineRenderer line = Instantiate(linePrefab);
                    lines.Add(line);
                    UpdateLinePosition(line, node.transform.position, targetNode.transform.position);

                    // Добавляем в словаре у наших узлов линии 
                    nodeConnections[node].Add(line);
                    nodeConnections[targetNode].Add(line);
                }
                attempts++;
            }
        }
    }

    // Получаем рандомный узел, который удовлетворяет условиям
    GameObject GetRandomNode(GameObject excludeNode)
    {
        List<GameObject> potentialNodes = new List<GameObject>();
        foreach (GameObject node in nodes)
        {
            // Проверяет, что узел не имеет больше определенного количества линий
            if (nodeConnections[node].Count < lineOneNode && node != excludeNode)
                potentialNodes.Add(node);
        }
        // Возвращаем случайный узел из списка, если список не пустой
        return potentialNodes.Count > 0 ? potentialNodes[Random.Range(0, potentialNodes.Count)] : null;
    }

    // Проверка, что узел А и узел Б уже не соединены
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

    // Обновляем линии, выставляем конечные позиции
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

    // Получаем второй узел на линии
    GameObject GetOtherNode(LineRenderer line, GameObject currentNode)
    {
        GameObject? endNode = nodes.Find(node => nodeConnections[node].Contains(line));

        if (endNode != null) return endNode;
        else return null;
    }

    // Обновляем позицию линии
    void UpdateLinePosition(LineRenderer line, Vector3 start, Vector3 end)
    {
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    // Обновляем цвет линии при определенном условии
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
        // Если все линии выставлены правильно вызывается метод завершающий уровень
        if (allLinesClear && !levelComplete) 
        {
            CompleteLevel();
        }
    }
    // Проверяем, пересекаются ли линии
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

        // Проверка определителя 
        float d = (q2.y - q1.y) * (p2.x - p1.x) - (q2.x - q1.x) * (p2.y - p1.y);
        // Линии не пересекаются
        if (d == 0) return false;

        //Ищем точки пересечения
        float u = ((q2.x - q1.x) * (p1.y - q1.y) - (q2.y - q1.y) * (p1.x - q1.x)) / d;
        float v = ((p2.x - p1.x) * (p1.y - q1.y) - (p2.y - p1.y) * (p1.x - q1.x)) / d;

        // Если точка не входит в промежуток (0;1) то линии не пересекаются + небольшая погрешность
        return (u - 0.01f) > 0 && (u + 0.01f) < 1 && (v - 0.01f) > 0 && (v + 0.01f) < 1;
    }

    Task CompleteLevel()
    {
        levelComplete = true;
        Debug.Log("Level Complete!");

        // Открытие панели, показ анимации
        levelComplatePanel.SetActive(true);
        // Запускаю уже готовую анимацию, еслиб ее не было, можно было бы попробовать через DoTween красиво оформить
        levelComplatePanel.GetComponent<Animator>().Play("LevelComplateAnimator");


        score += 350;
        scoreText.text = "Score: " + score;
        // Дополнительно: анимации, эффекты
        return Task.CompletedTask;
    }

    public async void SkipLevel()
    {
        Debug.Log("Level Skipped");

        await CompleteLevel();
        await Task.Delay(3000);

        FindObjectOfType<SceneController>().LoadScene(0);
    }

    // Отрисовка игрового поля, для удобной настройки в редакторе
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(playAreaSize.x, playAreaSize.y, 0));
    }

}
