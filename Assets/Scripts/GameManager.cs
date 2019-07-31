using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Snake
{
    public class GameManager : MonoBehaviour
    {
        public int mapHeight = 14;
        public int mapWidth = 17;
        public Color mapColorLight;
        public Color mapColorDark;
        public Color snakeColor;
        public Color snakeTailColor;
        public Color appleColor;
        public GameObject mainCamera;
        public float speed = 0.15f;
        public TMP_Text gameOverText;
        public TMP_Text restartText;
        public TMP_Text startText;
        public TMP_Text scoreText;
        public TMP_Text recordText;

        GameObject mapObject;
        SpriteRenderer mapRenderer;
        Node[,] grid;
        List<Node> availableNodes = new List<Node>();

        GameObject playerObject;
        GameObject appleObject;
        Queue<GameObject> tailObjects = new Queue<GameObject>();
        Node playerNode;
        Node appleNode;
        Queue<Node> tailNodes = new Queue<Node>();
        Vector2Int playerDirection = Vector2Int.right;
        Vector2Int lastPlayerDirection = Vector2Int.right;
        float timer;
        int applesEaten = 0;
        int applesRecord = 0;
        bool running = false;
        bool isPreGame = true;
        AudioSource audioSource;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            CreateMap();
            CenterCamera();
            PreGame();
        }

        void PreGame()
        {
            gameOverText.enabled = false;
            restartText.enabled = false;
            timer = 0;
            applesEaten = 0;
            UpdateScores();
        }

        void NewGame()
        {
            AddAvailableNodes(grid);
            CreateTailSegement(1, 3);
            CreateTailSegement(2, 3);
            PlacePlayer();
            CreateApple();
            gameOverText.enabled = false;
            restartText.enabled = false;
            timer = 0;
            applesEaten = 0;
            UpdateScores();
            playerDirection = Vector2Int.right;
            lastPlayerDirection = Vector2Int.right;
            running = true;
        }

        void ClearGame()
        {
            tailNodes.Clear();

            foreach (GameObject tailObject in tailObjects)
            {
                Destroy(tailObject);
            }

            tailObjects.Clear();
            availableNodes.Clear();

            Destroy(appleObject);
            Destroy(playerObject);

            gameOverText.enabled = true;
            restartText.enabled = true;
            running = false;
        }

        void CenterCamera()
        {
            mainCamera.transform.position += new Vector3(mapWidth * 0.5f, mapHeight * 0.5f - 0.75f, 0);
        }

        void GetDirection()
        {
            if (running)
            {
                if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
                {
                    if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > Mathf.Abs(Input.GetAxisRaw("Vertical")))
                    {
                        if (Input.GetAxisRaw("Horizontal") > 0 && lastPlayerDirection != Vector2Int.left)
                        {
                            playerDirection = Vector2Int.right;
                        }
                        else if (Input.GetAxisRaw("Horizontal") < 0 && lastPlayerDirection != Vector2Int.right)
                        {
                            playerDirection = Vector2Int.left;
                        }
                    }
                    else
                    {
                        if (Input.GetAxisRaw("Vertical") > 0 && lastPlayerDirection != Vector2Int.down)
                        {
                            playerDirection = Vector2Int.up;
                        }
                        else if (Input.GetAxisRaw("Vertical") < 0 && lastPlayerDirection != Vector2Int.up)
                        {
                            playerDirection = Vector2Int.down;
                        }
                    }
                }
            }
            else
            {
                if (isPreGame)
                {
                    if (Input.GetButtonDown("Restart"))
                    {
                        isPreGame = false;
                        startText.enabled = false;
                        NewGame();
                    }
                }
                else if (Input.GetButtonDown("Restart"))
                {
                    NewGame();
                }
            }
        }

        void MovePlayer()
        {
            Node targetNode = GetNode(playerNode.point.x + playerDirection.x, playerNode.point.y + playerDirection.y);

            lastPlayerDirection = playerDirection;

            if (targetNode == null || tailNodes.Contains(targetNode))
            {
                GameOver();
            }
            else
            {
                if (targetNode == appleNode)
                {
                    audioSource.Play();
                    availableNodes.Add(playerNode);
                    availableNodes.Remove(targetNode);

                    CreateTailSegement(playerNode.point.x, playerNode.point.y);

                    RandomlyPlaceApple();

                    applesEaten++;

                    if (applesEaten > applesRecord)
                    {
                        applesRecord = applesEaten;
                    }

                    UpdateScores();
                }
                else
                {
                    if (tailObjects.Count > 0)
                    {
                        CreateTailSegement(playerNode.point.x, playerNode.point.y);

                        availableNodes.Add(tailNodes.Dequeue());
                        Destroy(tailObjects.Dequeue());
                    }
                }

                playerNode = targetNode;
                playerObject.transform.position = targetNode.worldPosition;
                playerObject.transform.position += new Vector3(-0.05f, -0.05f, 0);
            }
        }

        void UpdateScores()
        {
            scoreText.text = "Apples " + applesEaten;
            recordText.text = "Record " + applesRecord;
        }

        void GameOver()
        {
            ClearGame();
        }

        void Update()
        {
            GetDirection();
        }

        void FixedUpdate()
        {
            if (running)
            {
                timer += Time.fixedDeltaTime;

                if (timer > speed)
                {
                    timer -= speed;
                    MovePlayer();
                }
            }
        }

        void CreateMap()
        {
            mapObject = new GameObject("Map");
            mapRenderer = mapObject.AddComponent<SpriteRenderer>();

            grid = new Node[mapWidth, mapHeight];

            Texture2D mapTexture = new Texture2D(mapWidth, mapHeight);

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Node node = new Node()
                    {
                        point = new Vector2Int(x, y),
                        worldPosition = new Vector3(x, y, 0)
                    };

                    grid[x, y] = node;

                    if ((x + y) % 2 == 0)
                    {
                        mapTexture.SetPixel(x, y, mapColorLight);
                    }
                    else
                    {
                        mapTexture.SetPixel(x, y, mapColorDark);
                    }
                }
            }

            AddAvailableNodes(grid);

            mapTexture.filterMode = FilterMode.Point;
            mapTexture.Apply();

            Rect mapRectangle = new Rect(0, 0, mapWidth, mapHeight);
            mapRenderer.sprite = Sprite.Create(mapTexture, mapRectangle, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
        }

        void AddAvailableNodes(Node[,] nodes)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    availableNodes.Add(nodes[x, y]);
                }
            }
        }

        Node GetNode(int x, int y)
        {
            if (x < 0 || x > mapWidth - 1 || y < 0 || y > mapHeight - 1)
            {
                return null;
            }

            return grid[x, y];
        }

        void PlacePlayer()
        {
            playerObject = new GameObject("Player");
            SpriteRenderer playerRenderer = playerObject.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = CreateSprite(snakeColor);
            playerRenderer.sortingOrder = 2;

            playerNode = GetNode(3, 3);
            playerObject.transform.position = playerNode.worldPosition;
            playerObject.transform.localScale = Vector3.one * 1.1f;
            playerObject.transform.position += new Vector3(-0.05f, -0.05f, 0);
            availableNodes.Remove(playerNode);
        }

        void CreateTailSegement(int x, int y)
        {
            GameObject tailObject = new GameObject("Tail Segment");
            SpriteRenderer tailRenderer = tailObject.AddComponent<SpriteRenderer>();
            tailRenderer.sprite = CreateSprite(snakeTailColor);
            tailRenderer.sortingOrder = 2;

            Node tailNode = GetNode(x, y);
            tailObject.transform.position = tailNode.worldPosition;
            tailObject.transform.localScale = Vector3.one * 0.85f;
            tailObject.transform.position += new Vector3(0.075f, 0.075f, 0);
            availableNodes.Remove(tailNode);

            tailObjects.Enqueue(tailObject);
            tailNodes.Enqueue(tailNode);
        }

        void CreateApple()
        {
            appleObject = new GameObject("Apple");
            SpriteRenderer appleRenderer = appleObject.AddComponent<SpriteRenderer>();
            appleRenderer.sprite = CreateSprite(appleColor);
            appleRenderer.sortingOrder = 1;

            RandomlyPlaceApple();

            availableNodes.Remove(appleNode);
        }

        void RandomlyPlaceApple()
        {
            appleNode = GetAppleNode();
            appleObject.transform.position = appleNode.worldPosition;
        }

        Node GetAppleNode()
        {
            if (availableNodes.Count == 0)
            {
                GameOver();
            }

            return availableNodes[Random.Range(0, availableNodes.Count)];
        }

        Sprite CreateSprite(Color targetColor)
        {
            Texture2D texture = new Texture2D(1, 1);

            texture.SetPixel(0, 0, targetColor);
            texture.filterMode = FilterMode.Point;
            texture.Apply();

            Rect rectangle = new Rect(0, 0, 1, 1);

            return Sprite.Create(texture, rectangle, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
        }
    }
}
