using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Assets.Scripts;


public class GameManager : MonoBehaviour
{

    const int maxPlayers = 2;
    const int boardSize = 9;
    const int minPlayers = 2;
    int numPlayers = 2;

    public Camera cam;

    public GameObject[] players;
    private PlayerInfo[] playerStatus;

    public GameObject[,] board = new GameObject[boardSize, boardSize];
    private gameSquareInfo[,] boardStatus;

    public GameObject[,] pegs = new GameObject[boardSize - 1, boardSize - 1];
    private WallPeg[,] wallPegStatus;

    public GameObject GamePiece1;
    public GameObject GamePiece2;
    public GameObject PegPiece;
    public GameObject wallH;
    public GameObject wallV;
    public GameObject gmPanel;

    public float startDelay = 1f;
    public float aiDelay = 10f;
    private WaitForSeconds m_StartWait;

    int totalWalls = 20;
    bool gameOver = false;
    int[,] accessible;

    public Text playersTurnText;
    public Text wallsRemainText;
    public Text WinnerText;
    public Text MessageText;

    Ray ray;
    RaycastHit hit;

    Assets.Scripts.Board MainBoard;

    private Assets.Scripts.Agent MyAgent;

    void Start()
    {
        numPlayers = MainMenu.playerTotal;

        MainBoard = new Board(numPlayers);
        playerStatus = MainBoard.playerStatus;
        Debug.Assert(playerStatus != null);
        wallPegStatus = MainBoard.wallPegStatus;
        Debug.Assert(wallPegStatus != null);
        boardStatus = MainBoard.boardStatus;
        Debug.Assert(boardStatus != null);
        accessible = MainBoard.accessible;
        Debug.Assert(accessible != null);

        gmPanel.SetActive(false);
        MessageText.text = "";
        MakeBoard();
        SpawnPlayers();

        m_StartWait = new WaitForSeconds(startDelay);
        StartCoroutine(GameLoop());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    void MakeBoard()
    {
        bool switchpiece = true;
        Vector3 currentPos = new Vector3(1f, 0f, 0f);
        Vector3 pegPos = new Vector3(2f, -1f, -0.75f);
        Quaternion pegRot = Quaternion.Euler(90, 0, 0);

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (i < 8 && j < 8)
                {
                    pegs[i, j] = Instantiate(PegPiece, pegPos, pegRot);
                    wallPegStatus[i, j] = pegs[i, j].GetComponent<WallPeg>();
                    wallPegStatus[i, j].x = i;
                    wallPegStatus[i, j].y = j;
                }
                if (switchpiece)
                {

                    board[i, j] = Instantiate(GamePiece1, currentPos, Quaternion.identity);
                    boardStatus[i, j] = board[i, j].GetComponent<gameSquareInfo>();
                    boardStatus[i, j].location = currentPos;
                    boardStatus[i, j].x = i;
                    boardStatus[i, j].y = j;
                }
                else
                {

                    board[i, j] = Instantiate(GamePiece2, currentPos, Quaternion.identity);
                    boardStatus[i, j] = board[i, j].GetComponent<gameSquareInfo>();
                    boardStatus[i, j].location = currentPos;
                    boardStatus[i, j].x = i;
                    boardStatus[i, j].y = j;
                }
                switchpiece = !switchpiece;
                currentPos += new Vector3(2f, 0, 0);
                pegPos += new Vector3(2f, 0, 0);
            }
            currentPos += new Vector3(-18f, -2f, 0);
            pegPos += new Vector3(-18f, -2f, 0);
        }
    }

    void SpawnPlayers()
    {
        for (int i = 0; i < numPlayers; i++)
        {
            playerStatus[i] = players[i].GetComponent<PlayerInfo>();
            playerStatus[i].body = Instantiate(players[i], playerStatus[i].spawnPoint, Quaternion.identity) as GameObject;
            playerStatus[i] = playerStatus[i].body.GetComponent<PlayerInfo>();
            playerStatus[i].body = players[i].gameObject;
            playerStatus[i].id = i + 1;
        }

 
        if (numPlayers == 2)
        {
            playerStatus[0].transform.position = playerStatus[0].spawnPoint;
            playerStatus[0].x = 8;
            playerStatus[0].y = 4;
            playerStatus[0].goalX = 0;
            playerStatus[0].goalY = -1;
            boardStatus[8, 4].isOpen = false;
            playerStatus[1].transform.position = playerStatus[1].spawnPoint;
            playerStatus[1].x = 0;
            playerStatus[1].y = 4;
            playerStatus[1].goalX = 8;
            playerStatus[1].goalY = -1;
            boardStatus[0, 4].isOpen = false;
            if (MainMenu.playerSettings == 1)
            {
                playerStatus[1].isAi = true;
                MyAgent = new Assets.Scripts.Agent();
            }
        }
    }

    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(GamePrep());
        yield return StartCoroutine(PlayGame());
    }

    private IEnumerator GamePrep()
    {
        int wallAmt = totalWalls / numPlayers;
        for (int i = 0; i < numPlayers; i++)
        {
            playerStatus[i].wallsLeft = wallAmt;
            wallsRemainText.text += "Player " + (i + 1) + "'s Walls: " + playerStatus[i].wallsLeft + "\n";
        }
        yield return null;
    }

    private IEnumerator PlayGame()
    {
        int turnOrder = 0;

        while (!gameOver)
        {
            if (!playerStatus[turnOrder].isAi)
                yield return StartCoroutine(PlayersTurn(turnOrder));
            else
                yield return StartCoroutine(AITurn(turnOrder));
            if (playerStatus[turnOrder].CheckWin())
            {

                gameOver = true;
                GameOver(turnOrder + 1);
                break;
            }
            if (++turnOrder == numPlayers)
                turnOrder = 0;
        }

        yield return null;
    }
    private IEnumerator AITurn(int playerNum)
	{
        MessageText.text = "";
        playerStatus[playerNum].currentTurn = true;
        playersTurnText.text = "Player " + (playerNum + 1) + "'s Turn!";
        Assets.Scripts.ActionFunction action = MyAgent.NextMove(MainBoard, playerNum); 
        if (action.function == null)
        {
            Debug.LogError("Agent action is null. Something is wrong. Agent just skip his move");
            yield break;
        }
        if (action.function.Method.Name == "MovePawn")
            RenderPawnPosition(action.player, action.x, action.y);
        else if (action.function.Method.Name == "PlaceHorizontalWall")
            RenderWall(action.x, action.y, true);
        else if (action.function.Method.Name == "PlaceVerticalWall")
            RenderWall(action.x, action.y, false);
        else
            Debug.LogError("Agent returning a non-supported action");
        MainBoard.ExecuteFunction(action);
        UpdateWallRemTxt();
        yield return null;
    }

    private IEnumerator PlayersTurn(int p) 
    {
        MessageText.text = "";

        playerStatus[p].currentTurn = true;
        playersTurnText.text = "Player " + (p + 1) + "'s Turn!";
        while (playerStatus[p].currentTurn)
        {

            if (Input.GetKeyUp(KeyCode.X))
            {
                playersTurnText.text = "Player " + (p + 1) + " Passes!";
                playerStatus[p].currentTurn = false;
                yield return m_StartWait;
            }

			ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			bool collisionFound = Physics.Raycast(ray, out hit);

			if (Input.GetMouseButtonUp(0))
            {
                GameObject tempObj;
                if (collisionFound)
                {
                    if (hit.transform.tag == "Board")
                    {
                        gameSquareInfo tempSquare;
                        int xDiff;
                        int yDiff;
                        tempObj = hit.collider.gameObject;
                        tempSquare = tempObj.GetComponent<gameSquareInfo>();

                        xDiff = tempSquare.x - playerStatus[p].x;
                        yDiff = tempSquare.y - playerStatus[p].y;

                        if (tempSquare.isOpen)
                        {
                            if (xDiff == 1 && yDiff == 0 &&
                                MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }
                            else if (xDiff == 2 && yDiff == 0 &&
								MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }
                            else if (xDiff == 1 && yDiff == -1 &&
                                MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }

                            else if (xDiff == -1 && yDiff == 0 &&
                             MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }
                            else if (xDiff == -2 && yDiff == 0 &&
                             MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }
                            else if (xDiff == -1 && yDiff == 1 &&
                                MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;

                            }

                            else if (xDiff == 0 && yDiff == -1 &&
                             MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }
                            else if (xDiff == 0 && yDiff == -2 &&
								MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }
                            else if (xDiff == -1 && yDiff == -1 &&
                                MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;

                            }

                            else if (xDiff == 0 && yDiff == 1 &&
                             MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }
                            else if (xDiff == 0 && yDiff == 2 &&
                             MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }
                            else if (xDiff == 1 && yDiff == 1 &&
                             MainBoard.IsPawnMoveLegal(playerStatus[p].x, playerStatus[p].y, tempSquare.x, tempSquare.y))
                            {
                                RenderPawnPosition(p, tempSquare.x, tempSquare.y);
                                Board.MovePawn(MainBoard, p, tempSquare.x, tempSquare.y);
                                playerStatus[p].currentTurn = false;
                            }
                        }

                    } 

                    if (hit.transform.tag == "Peg")
                    {
                        WallPeg tempPeg;
                        tempObj = hit.collider.gameObject;
                        tempPeg = tempObj.GetComponent<WallPeg>();

                        if (playerStatus[p].wallsLeft > 0 && MainBoard.CheckWallH(tempPeg.x, tempPeg.y))
                        {
                            RenderWall(tempPeg.x, tempPeg.y, true);
                            Board.PlaceHorizontalWall(MainBoard, p, tempPeg.x, tempPeg.y);
                            playerStatus[p].currentTurn = false;
                            UpdateWallRemTxt();
                        }
                    }
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                if (collisionFound)
                {
                    GameObject tempObj;
                    if (hit.transform.tag == "Peg")
                    {
                        WallPeg tempPeg;
                        tempObj = hit.collider.gameObject;
                        tempPeg = tempObj.GetComponent<WallPeg>();

                        if (playerStatus[p].wallsLeft > 0 && MainBoard.CheckWallV(tempPeg.x, tempPeg.y))
                        {
                            RenderWall(tempPeg.x, tempPeg.y, false);
                            Board.PlaceVerticalWall(MainBoard, p, tempPeg.x, tempPeg.y);
                            playerStatus[p].currentTurn = false;
                            UpdateWallRemTxt();
                        }
                    }
                }
            }


            yield return null;
        }
    }

    void GameOver(int playerNum)
    {
        gmPanel.SetActive(true);
        WinnerText.text = "Player " + (playerNum) + " is the Winner!";
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(0);
    }

    public void RenderWall(int xPos, int yPos, bool isHorizontal)
    {
        if (isHorizontal)
            Instantiate(wallH, wallPegStatus[xPos, yPos].transform.position, Quaternion.identity);
        else
        {
            Quaternion wallRot = Quaternion.Euler(0, 0, 90);
            Instantiate(wallV, wallPegStatus[xPos, yPos].transform.position, wallRot);
        }
    }


    public void RenderPawnPosition(int player, int xPos, int yPos)
    {
        playerStatus[player].transform.position = boardStatus[xPos, yPos].transform.position + new Vector3(0, 0, -1f);
    }

    void UpdateWallRemTxt()
    {
        wallsRemainText.text = "Remaining Walls:\n\n";
        for (int i = 0; i < numPlayers; i++)
        {
            wallsRemainText.text += "Player " + (i + 1) + "'s Walls: " + playerStatus[i].wallsLeft + "\n";
        }
    }
}
