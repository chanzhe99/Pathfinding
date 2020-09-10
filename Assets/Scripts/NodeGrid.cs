using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NodeGrid : MonoBehaviour
{
    Vector2 GridStartPosition = new Vector2(-5.0f, -5.0f); // Start position for first cell
    float NodeSize; // Size of individual cell

    #region Grid
    [Header("Grid Settings")]
    [Space(5)]
    [SerializeField] int GridCount = 50;
    [SerializeField] GameObject NodePrefab;
    Node[,] NodeArray;
    #endregion
    #region Pathfinding
    [Header("Pathfinding Settings")]
    [Space(5)]
    [SerializeField] bool Diagonal = false;
    [SerializeField] bool CrossCorners = false;
    List<Node> OpenList;
    List<Node> PathList;
    Node CurrentNode;
    Node EndNode;
    #endregion
    #region Input
    Node.State clickedState; // Check what state the node is when first clicked
    Node PreviousSelectedNode;
    #endregion
    #region AStar
    [Header("A Star Settings")]
    [Space(5)]
    [SerializeField] bool AStar = false;
    [SerializeField] float AStarWeightMin = 0.0f;
    [SerializeField] float AStarWeightMax = 10.0f;
    [SerializeField] [Range(0.0f, 10.0f)] float AStarWeight = 1.0f;
    #endregion
    #region Simulation
    [Header("Simulation Speed Settings")]
    [Space(5)]
    [SerializeField] float SimSpeedMin = 1.0f;
    [SerializeField] float SimSpeedMax = 60.0f;
    [SerializeField] [Range(1.0f, 60.0f)] float SimSpeed = 60.0f;
    float Elapsed = 0.0f;
    bool Pathfinding = false;
    bool IsPaused = false;
    #endregion
    #region Debugging
    bool Debugging = false;
    #endregion
    #region UI
    [Header("UI")]
    [Space(5)]
    [SerializeField] Button ChangeAlgoButton;
    [SerializeField] TMP_Text ChangeAlgoText;

    [SerializeField] TMP_Text AStarWeightText;
    [SerializeField] Slider AStarWeightSlider;

    [SerializeField] Toggle DiagonalToggle;
    [SerializeField] TMP_Text DiagonalText;

    [SerializeField] Toggle CrossCornersToggle;
    [SerializeField] TMP_Text CrossCornersText;

    [SerializeField] TMP_Text StartPauseText;
    [SerializeField] TMP_Text ClearSearchText;

    [SerializeField] Button ClearWallsButton;
    [SerializeField] TMP_Text ClearWallsText;

    [SerializeField] TMP_Text SimSpeedText;
    [SerializeField] Slider SimSpeedSlider;

    [SerializeField] Toggle DebugToggle;
    [SerializeField] TMP_Text DebugText;

    [SerializeField] GameObject NoPathPanel;
    #endregion

    private void Start()
    {
        NodeArray = new Node[GridCount, GridCount];
        OpenList = new List<Node>();
        PathList = new List<Node>();

        NodeSize = 10.0f / GridCount;
        for (int y = 0; y < GridCount; y++)
        {
            for (int x = 0; x < GridCount; x++)
            {
                NodeArray[x, y] = Instantiate(NodePrefab, GridStartPosition + new Vector2(NodeSize * x, NodeSize * y), Quaternion.identity).GetComponent<Node>();
                NodeArray[x, y].transform.localScale = new Vector2(NodeSize, NodeSize);
                NodeArray[x, y].SetCoord(x, y);
            }
        }
        NodeArray[(GridCount/2) - 5, GridCount/2].SetState(Node.State.startPoint);
        NodeArray[(GridCount/2) + 5, GridCount/2].SetState(Node.State.endPoint);

        #region UI Setup
        // Change alogrithm
        ChangeAlgoButton.interactable = true;
        ChangeAlgoText.SetText("Algorithm:\nDijkstra");

        // A Star
        AStarWeightText.SetText(AStarWeight.ToString("Weight: 0"));
        AStarWeightText.color = Color.grey;
        AStarWeightSlider.interactable = false;
        AStarWeightSlider.minValue = AStarWeightMin;
        AStarWeightSlider.maxValue = AStarWeightMax;
        AStarWeightSlider.value = AStarWeight;

        // Diagonal toggle
        DiagonalToggle.isOn = Diagonal;

        // Cross corners toggle
        CrossCornersToggle.isOn = CrossCorners;
        CrossCornersToggle.interactable = false;
        CrossCornersText.color = Color.grey;

        // Start pause button
        StartPauseText.SetText("Start\nSearch");

        // Clear walls button
        ClearWallsButton.interactable = true;

        // Simulation speed
        SimSpeedText.SetText(SimSpeed.ToString("Sim Speed: 0.0/s"));
        SimSpeedSlider.minValue = SimSpeedMin;
        SimSpeedSlider.maxValue = SimSpeedMax;
        SimSpeedSlider.value = SimSpeed;

        // Debug toggle
        DebugToggle.isOn = Debugging;

        // No path panel
        NoPathPanel.SetActive(false);
        #endregion
    }
    
    #region Pathfinding functions
    void ResetGrid(int gridCount)
    {
        for (int y = 0; y < gridCount; y++)
        {
            for (int x = 0; x < gridCount; x++)
            {
                if (OpenList.Count != 0)
                    OpenList.Clear();

                if(NodeArray[x, y].GetParentNode() != null)
                    NodeArray[x, y].SetParentNode(null);
                if (NodeArray[x, y].GetFCost() != Mathf.Infinity)
                    NodeArray[x, y].SetFCost(Mathf.Infinity);
                if (NodeArray[x, y].GetGCost() != Mathf.Infinity)
                    NodeArray[x, y].SetGCost(Mathf.Infinity);
                if (NodeArray[x, y].GetHCost() != Mathf.Infinity)
                    NodeArray[x, y].SetHCost(Mathf.Infinity);
                if (NodeArray[x, y].GetSettled())
                    NodeArray[x, y].SetSettled(false);
                if(NodeArray[x, y].GetIsPath())
                    NodeArray[x, y].SetIsPath(false);
                if (NodeArray[x, y].GetChecking())
                    NodeArray[x, y].SetChecking(false);
            }
        }
    }
    void SetSourceNode(int gridCount)
    {
        bool sourceNodeFound = false;
        bool endNodeFound = false;

        for (int y = 0; y < gridCount; y++)
        {
            for (int x = 0; x < gridCount; x++)
            {
                if (NodeArray[x, y].GetState() == Node.State.startPoint && !sourceNodeFound)
                {
                    NodeArray[x, y].SetGCost(0.0f);
                    NodeArray[x, y].SetFCost(0.0f);
                    OpenList.Add(NodeArray[x, y]);
                    sourceNodeFound = true;
                }

                if (NodeArray[x, y].GetState() == Node.State.endPoint && !endNodeFound)
                {
                    EndNode = NodeArray[x, y];
                    endNodeFound = true;
                }

                if (sourceNodeFound && endNodeFound)
                    break;
            }
            if (sourceNodeFound && endNodeFound)
                break;
        }
    }
    void Pathfind()
    {
        CurrentNode = OpenList[0];
        int CurrentNodeIndex = 0;
        for (int i = 0; i < OpenList.Count; i++)
        {
            if ((!AStar && OpenList[i].GetGCost() < CurrentNode.GetGCost()) || (AStar && OpenList[i].GetFCost() < CurrentNode.GetFCost()))
            {
                CurrentNode = OpenList[i];
                CurrentNodeIndex = i;
            }
        }
        CurrentNode.SetSettled(true);
        OpenList.RemoveAt(CurrentNodeIndex);
        
        CheckNeighbours((int)CurrentNode.GetCoord().x, (int)CurrentNode.GetCoord().y);
    }
    void CheckNeighbours(int x, int y)
    {
        // Check cell above
        if (y != GridCount-1)
            UpdateNeighbour(x, 0, y, +1);

        // Check cell on right
        if (x != GridCount-1)
            UpdateNeighbour(x, +1, y, 0);

        // Check cell below
        if (y != 0)
            UpdateNeighbour(x, 0, y, -1);

        // Check cell on left
        if (x != 0)
            UpdateNeighbour(x, -1, y, 0);

        if(Diagonal)
        {
            // Check cell on top right
            if(x != GridCount - 1 && y != GridCount - 1)
                UpdateNeighbour(x, +1, y, +1);

            // Check cell on bottom right
            if (x != GridCount - 1 && y != 0)
                UpdateNeighbour(x, +1, y, -1);

            // Check cell on bottom left
            if (x != 0 && y != 0)
                UpdateNeighbour(x, -1, y, -1);
        
            // Check cell on top left
            if(x != 0 && y != GridCount - 1)
                UpdateNeighbour(x, -1, y, +1);
        }
    }
    void UpdateNeighbour(int x, int xOffset, int y, int yOffset)
    {
        if (OrthoOrDiag(x, xOffset, y, yOffset)) // OrthoOrDiag function declared further down
        {
            if (!OpenList.Contains(NodeArray[x + xOffset, y + yOffset]))
            {
                OpenList.Add(NodeArray[x + xOffset, y + yOffset]);
                NodeArray[x + xOffset, y + yOffset].SetChecking(true);
            }

            if (
            ((Mathf.Abs(xOffset) + Mathf.Abs(yOffset) == 1) && (NodeArray[x, y].GetGCost() + 10 < NodeArray[x + xOffset, y + yOffset].GetGCost())) ||
            ((Mathf.Abs(xOffset) + Mathf.Abs(yOffset) == 2) && (NodeArray[x, y].GetGCost() + 14 < NodeArray[x + xOffset, y + yOffset].GetGCost()))
            )
            {
                int cost = (Mathf.Abs(xOffset) + Mathf.Abs(yOffset) < 2) ? 10 : 14;
                NodeArray[x + xOffset, y + yOffset].SetParentNode(NodeArray[x, y]);
                NodeArray[x + xOffset, y + yOffset].SetGCost(NodeArray[x, y].GetGCost() + cost);
                if (AStar)
                    NodeArray[x + xOffset, y + yOffset].SetFCost(NodeArray[x + xOffset, y + yOffset].GetGCost() + CalculateHCost(x + xOffset, y + yOffset)); // CalculateHCost function declared further down
            }
        }
    }
    void RetracePath()
    {
        int CurrentNodeIndex = 0;
        PathList.Clear();

        PathList.Add(EndNode);
        CurrentNode = PathList[CurrentNodeIndex];

        while(true)
        {
            PathList[CurrentNodeIndex].GetParentNode().SetIsPath(true);
            PathList.Add(CurrentNode.GetParentNode());
            CurrentNodeIndex++;
            CurrentNode = PathList[CurrentNodeIndex];

            if (CurrentNode.GetGCost() == 0)
                break;
        }

        Pathfinding = false;
        StartPauseText.SetText("Restart\nSearch");
        ClearSearchText.SetText("Clear\nPath");
        ChangeInteractable(true);
        Debug.Log("Retraced");
    }
    bool OrthoOrDiag(int x, int xOffset, int y, int yOffset) {
        return ((Mathf.Abs(xOffset) + Mathf.Abs(yOffset) < 2) && NodeArray[x + xOffset, y + yOffset].GetState() != Node.State.blocked && !NodeArray[x + xOffset, y + yOffset].GetSettled()) ||
               ((Mathf.Abs(xOffset) + Mathf.Abs(yOffset) >= 2) && NodeArray[x + xOffset, y + yOffset].GetState() != Node.State.blocked &&
               (CrossCorners || (!CrossCorners && (NodeArray[x + xOffset, y].GetState() != Node.State.blocked || NodeArray[x, y + yOffset].GetState() != Node.State.blocked))) &&
               !NodeArray[x + xOffset, y + yOffset].GetSettled());
    }
    float CalculateHCost(int x, int y) { return AStarWeight * (10 * (Mathf.Abs(EndNode.GetCoord().x - x) + Mathf.Abs(EndNode.GetCoord().y - y))); }
    #endregion
    #region User input functions
    void ChangeInteractable(bool interactable)
    {
        if (ChangeAlgoButton.interactable != interactable)
            ChangeAlgoButton.interactable = interactable;
        if (AStar && AStarWeightSlider.interactable != interactable)
        {
            AStarWeightSlider.interactable = interactable;
            AStarWeightText.color = (interactable) ? Color.black : Color.grey;
        }
        if (DiagonalToggle.interactable != interactable)
        {
            DiagonalToggle.interactable = interactable;
            DiagonalText.color = (interactable) ? Color.black : Color.grey;
        }
        if (Diagonal && CrossCornersToggle.interactable != interactable)
        {
            CrossCornersToggle.interactable = interactable;
            CrossCornersText.color = (interactable) ? Color.black : Color.grey;
        }
        if (ClearWallsButton.interactable != interactable)
            ClearWallsButton.interactable = interactable;
    }
    public void ButtonStartPause()
    {
        if(!Pathfinding)
        {
            Pathfinding = true;
            if(IsPaused)
                IsPaused = false;
            ResetGrid(GridCount);
            SetSourceNode(GridCount);
            StartPauseText.SetText("Pause\nSearch");
            ClearSearchText.SetText("Cancel\nSearch");
            ChangeInteractable(false);
        }
        else
        {
            if (!IsPaused)
            {
                IsPaused = true;
                StartPauseText.SetText("Resume\nSearch");
            }
            else
            {
                IsPaused = false;
                StartPauseText.SetText("Pause\nSearch");
            }
        }
        if(NoPathPanel.activeSelf)
            NoPathPanel.SetActive(false);
    }
    public void ButtonCancelSearch()
    {
        if (Pathfinding)
            Pathfinding = false;
        if (IsPaused)
            IsPaused = false;
        ResetGrid(GridCount);
        StartPauseText.SetText("Start\nSearch");
        ClearSearchText.SetText("Cancel\nSearch");

        ChangeInteractable(true);
        if (NoPathPanel.activeSelf)
            NoPathPanel.SetActive(false);
    }
    public void ButtonClearWalls()
    {
        for (int y = 0; y < GridCount; y++)
        {
            for (int x = 0; x < GridCount; x++)
            {
                if (NodeArray[x, y].GetState() == Node.State.blocked)
                    NodeArray[x, y].SetState(Node.State.clear);
            }
        }
        ResetGrid(GridCount);
        if (NoPathPanel.activeSelf)
            NoPathPanel.SetActive(false);
    }
    public void ButtonChangeAlgo()
    {
            if (!AStar)
            {
                AStar = true;
                ChangeAlgoText.SetText("Algorithm:\nA*");
                AStarWeightText.color = Color.black;
                AStarWeightSlider.interactable = true;
            }
            else
            {
                AStar = false;
                ChangeAlgoText.SetText("Algorithm:\nDijkstra");
                AStarWeightText.color = Color.grey;
                AStarWeightSlider.interactable = false;
            }
    }
    public void SliderAStarWeight() { AStarWeight = AStarWeightSlider.value; }
    public void ToggleDiagonal()
    {
        CrossCornersToggle.interactable = Diagonal = DiagonalToggle.isOn;
        CrossCornersText.color = (CrossCornersToggle.interactable) ? Color.black : Color.grey;
    }
    public void ToggleCrossCorners() { CrossCorners = CrossCornersToggle.isOn; }
    public void SliderSimSpeed() { SimSpeed = SimSpeedSlider.value; }
    public void ButtonQuit() { Application.Quit(); }
    public void ToggleDebug() { Debugging = DebugToggle.isOn; }
    //public void Button
    #endregion

    private void Update()
    {
        if(!Pathfinding)
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (Input.GetMouseButtonDown(0) && hit.collider != null)
            {
                clickedState = hit.collider.GetComponent<Node>().GetState();
                PreviousSelectedNode = hit.collider.GetComponent<Node>();
            }

            if (Input.GetMouseButton(0) && hit.collider != null)
            {
                if (clickedState == Node.State.startPoint && hit.collider.GetComponent<Node>() != PreviousSelectedNode &&
                    hit.collider.GetComponent<Node>().GetState() != Node.State.blocked)
                {
                    PreviousSelectedNode.SetState(Node.State.clear);
                    PreviousSelectedNode = hit.collider.GetComponent<Node>();
                    PreviousSelectedNode.SetState(Node.State.startPoint);
                }
                else if (clickedState == Node.State.endPoint && hit.collider.GetComponent<Node>() != PreviousSelectedNode &&
                    hit.collider.GetComponent<Node>().GetState() != Node.State.blocked)
                {
                    PreviousSelectedNode.SetState(Node.State.clear);
                    PreviousSelectedNode = hit.collider.GetComponent<Node>();
                    PreviousSelectedNode.SetState(Node.State.endPoint);
                }
                else if (hit.collider.GetComponent<Node>().GetState() != Node.State.startPoint && hit.collider.GetComponent<Node>().GetState() != Node.State.endPoint)
                {
                    if (clickedState == Node.State.clear)
                        hit.collider.GetComponent<Node>().SetState(Node.State.blocked);
                    else if (clickedState == Node.State.blocked)
                        hit.collider.GetComponent<Node>().SetState(Node.State.clear);
                }
            }
        }

        AStarWeightText.SetText(AStarWeight.ToString("Weight: 0"));
        SimSpeedText.SetText(SimSpeed.ToString("Sim Speed: 0.0/s"));
        for (int y = 0; y < GridCount; y++)
        {
            for (int x = 0; x < GridCount; x++)
            {
                NodeArray[x, y].SetDebugging(Debugging);
            }
        }

        // Slows down update cycle
        Elapsed += Time.deltaTime;
        if (Elapsed < 1 / SimSpeed) return;
        Elapsed -= 1 / SimSpeed;

        if (Pathfinding && !IsPaused)
        {
            if (!EndNode.GetSettled() && OpenList.Count != 0)
                Pathfind();
            else if(EndNode.GetSettled())
                RetracePath();
            else
            {
                if(Pathfinding)
                    Pathfinding = false;
                if(IsPaused)
                    IsPaused = false;
                StartPauseText.SetText("Restart\nSearch");
                ClearSearchText.SetText("Clear\nSearch");
                ChangeInteractable(true);
                NoPathPanel.SetActive(true);
                Debug.Log("No Path Found");
            }
        }
        
    }
}
