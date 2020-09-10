using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Node : MonoBehaviour
{
    SpriteRenderer NodeSprite;
    [SerializeField] TextMesh NodeText;

    public enum State
    {
        clear = 0,
        blocked,
        startPoint,
        endPoint
    };
    State NodeState = 0;

    [SerializeField] Vector2 Coord;

    Node ParentNode;
    float FCost = Mathf.Infinity;
    float GCost = Mathf.Infinity;
    float HCost = Mathf.Infinity;
    bool Settled = false;
    bool Checking = false;
    bool IsPath = false;
    bool Debugging = false;

    private void Awake()
    {
        NodeSprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if(Debugging)
        {
            if (GCost == Mathf.Infinity)
                NodeText.text = "-";
            else
                NodeText.text = GCost.ToString();
        }
        else
            NodeText.text = "";
    }

    public State GetState() { return NodeState; }
    public void SetState(State newState)
    {
        NodeState = newState;

        switch (NodeState)
        {
            case State.clear:
                NodeSprite.color = Color.white;
                break;
            case State.blocked:
                NodeSprite.color = Color.black;
                break;
            case State.startPoint:
                NodeSprite.color = Color.green;
                break;
            case State.endPoint:
                NodeSprite.color = Color.red;
                break;
        }

        //if(GCost)
    }

    public Vector2 GetCoord() { return Coord; }
    public void SetCoord(int x, int y) { Coord.x = x; Coord.y = y; }

    public float GetFCost() { return FCost; }
    public void SetFCost(float fCost) { FCost = fCost; }

    public float GetGCost() { return GCost; }
    public void SetGCost(float gCost) { GCost = gCost; }

    public float GetHCost() { return HCost; }
    public void SetHCost(float hCost) { HCost = hCost; }

    public bool GetSettled() { return Settled; }
    public void SetSettled(bool settled)
    {
        Settled = settled;
        if(NodeState == State.clear)
        {
            if (settled)
                NodeSprite.color = Color.blue;
            else
                NodeSprite.color = Color.white;
        }
    }

    public Node GetParentNode() { return ParentNode; }
    public void SetParentNode(Node parentNode) { ParentNode = parentNode; }

    public bool GetIsPath() { return IsPath; }
    public void SetIsPath(bool isPath)
    {
        IsPath = isPath;
        if (isPath && NodeState == State.clear)
            NodeSprite.color = Color.cyan;
    }

    public bool GetChecking() { return Checking; }
    public void SetChecking(bool checking)
    {
        Checking = checking;
        if(NodeState == State.clear)
        {
            if (checking)
                NodeSprite.color = Color.yellow;
            else
                NodeSprite.color = Color.white;
        }
    }

    public bool GetDebugging() { return Debugging; }
    public void SetDebugging(bool debugging) { Debugging = debugging; }
}