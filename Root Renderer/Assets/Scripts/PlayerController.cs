using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using World;
using Growth;

public class PlayerController : MonoBehaviour
{
    
    // [SerializeField] private LineRenderer lineRenderer;

    // private float _distanceTraveled = 0f;
    // private int _lineRendererPointIndex = 0;
    
    // private Root _root;
    // private float _rootWidth = 1f;
    public bool IsGrounded = false;

    [SerializeField] private Tilemap dirtTilemap;

    [Header("World")]
    [SerializeField] private WorldGenerator worldGenerator;

    [Header("Camera")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private List<Vector3> recapLine = new List<Vector3>();
    
    [Header("Player")]
    [SerializeField] private float _rootPointSpacing = 1f;
    
    [Header("Physics")]
    [SerializeField] private CapsuleCollider2D collider2D;
    [SerializeField] private Rigidbody2D rigidbody2D;
    [SerializeField] private float floatingGravity = 0.25f;

    [Header("Roots")]
    [SerializeField] private GameObject rootPrefab;
    [SerializeField] private Transform rootParent;
    
    [SerializeField] private Root _parentRoot;
    [SerializeField] private Root _activeRoot;
    private LineRenderer _activeRootRenderer;
    private int _activeRootPointIndex = 0;
    private float _activeRootDistanceTraveled = 0f;
    private float _activeRootWidth = 1f;
    private bool _parentSpawned = false;

    [Header("Energy")]
    [SerializeField] private float _energyBurnRate = 1f;
    [SerializeField] private float _totalEnergy = 100f;
    [SerializeField] private float _extraEnergy = 0f;

    [Header("UI")]
    [SerializeField] private UIController uiController;
    [SerializeField] private Slider energySlider;
    [SerializeField] private Slider extraEnergySlider;
    [SerializeField] private TextMeshProUGUI rootCounter;

    [Header("Tree")]
    [SerializeField] private Growth.Tree tree;

    [Header("Game")]
    public int Days = 0;

    void Start()
    {
        StartDay();
    }
    
    void FixedUpdate()
    {
        // Only move if there is energy left to move
        if (_extraEnergy > 0f || _totalEnergy > 0f)
        {
            // Collect the inputs
            float rotation = Input.GetAxis("Horizontal");
            
            // Apply gravity if not grounded
            if (IsGrounded)
            {
                rigidbody2D.gravityScale = 0f;
            }
            else
            {
                rigidbody2D.gravityScale = floatingGravity;
            }
        
            // Rotate the player
            Quaternion quaternion = transform.rotation;
            Vector3 rotationAngles = transform.eulerAngles;
            rotationAngles.z += rotation * (tree.TreeStats.GrowthManuverability.GetValue() * Time.fixedDeltaTime);
            quaternion.eulerAngles = rotationAngles;
            rigidbody2D.MoveRotation(quaternion);
            // transform.eulerAngles = rotationAngles;
        
            // Get the down vector that will act as the forward vector of movement for the player
            Vector3 movementVector = -transform.up * (tree.TreeStats.GrowthSpeed.GetValue() * Time.fixedDeltaTime);
            
            // Add the gravity if any
            Vector2 gravityVector = Physics2D.gravity * (rigidbody2D.gravityScale * Time.fixedDeltaTime);
            movementVector += (Vector3)gravityVector;
            
            // Check if the ground is changing the movement speed
            TerrainType footTerrain = worldGenerator.GetTerrainType(transform.position);
            
            // Change the movement speed if needed
            switch (footTerrain)
            {
                case TerrainType.Gravel:
                    movementVector *= 0.5f;
                    break;
                case TerrainType.RichSoil:
                    movementVector *= 1.25f;
                    break;
                default:
                    break;
            }
        
            // Add the distance traveled
            _activeRootDistanceTraveled += movementVector.magnitude;

            // Move the player position
            rigidbody2D.MovePosition(transform.position + movementVector);
            // transform.position += movementVector;
            
            // Check if the tile is grounded
            IsGrounded = IsRootGrounded();

            // Burn energy
            if (_extraEnergy <= 0f)
            {
                _totalEnergy -= _energyBurnRate * Time.fixedDeltaTime;
                if (_totalEnergy <= 0f)
                {
                    _totalEnergy = 0f;
                }
            }
            else
            {
                _extraEnergy -= _energyBurnRate * Time.fixedDeltaTime;
                if (_extraEnergy < 0f)
                {
                    _totalEnergy -= Mathf.Abs(_extraEnergy);
                    _extraEnergy = 0f;
                }
            }
            
            // Set the width
            _activeRootWidth = _totalEnergy / tree.TreeStats.Energy.GetValue();
            
            // Update the collider
            UpdateCollider();
        
            // If the distance traveled divided by the point spacing is greater than the current point index, add a new point to the line renderer
            if (Mathf.FloorToInt(_activeRootDistanceTraveled / _rootPointSpacing) > _activeRootPointIndex)
            {
                // Create a new root point
                RootPoint rootPoint = new RootPoint(transform.position, (Mathf.Round(_activeRootWidth * 100f) / 100f) + 0.1f);
                
                // Add the root point to the root
                _activeRoot.AddRootPoint(rootPoint);
                
                // Add the point to the line renderer
                _activeRootPointIndex++;
                _activeRootRenderer.positionCount = _activeRootPointIndex;

                _activeRootRenderer.SetPosition(_activeRootPointIndex-1, transform.position);
                
                // Set the width of the line renderer
                _activeRootRenderer.widthCurve = _activeRoot.WidthCurve;
            }
            
            // Update the root score
            tree.RootScore = GetActiveRootScore();
            
            // Check if the root is finished growing
            if (_extraEnergy <= 0f && _totalEnergy <= 0f)
            {
                // Set the rigidbody gravity scale to 0
                rigidbody2D.gravityScale = 0f;
                
                // Check for ungrown child roots
                Root ungrownRoot = _activeRoot.GetUngrownRoot();

                if (!ungrownRoot.HasParent && ungrownRoot != _parentRoot)
                {
                    // There are no ungrown roots, the day is finished
                    Debug.Log("Root Growth finished!");
                    FinishDay();
                }
                else
                {
                    // Start growing the ungrown root
                    SetActiveRoot(ungrownRoot);
                }
            }
        }
        
        // Update the energy slider visuals
        // UpdateEnergySliders();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Powerup"))
        {
            PowerupType powerupType = other.gameObject.GetComponent<Powerup>().PowerupType;

            switch (powerupType)
            {
                case PowerupType.Split:
                    // Create a child root here for later
                    GenerateRoot(transform.position, _activeRootWidth);
                    break;
                default:
                    break;
            }
            
            // Destroy the powerup
            Destroy(other.gameObject, 0f);
        }

        switch (other.gameObject.tag)
        {
            case "Powerup":
                // Powerup
                PowerupType powerupType = other.gameObject.GetComponent<Powerup>().PowerupType;

                switch (powerupType)
                {
                    case PowerupType.Split:
                        // Create a child root here for later
                        GenerateRoot(transform.position, _activeRootWidth);
                        break;
                    default:
                        break;
                }
            
                // Destroy the powerup
                Destroy(other.gameObject, 0f);
                break;
            case "Stat Card":
                // Stat Card
                TreeCard treeCard = other.gameObject.GetComponent<TreeCard>();
                // Add the stat card to the modifiers of the tree
                tree.TreeStats.AddModifer(treeCard.StatType, treeCard.Modifier);

                if (treeCard.StatType == StatType.Energy)
                {
                    // Add the energy if its a flat bonus
                    if (treeCard.Modifier.OperationType == StatOperation.Addition)
                    {
                        _extraEnergy += treeCard.Modifier.Value;
                    }
                }
                
                // Destroy the Stat Card
                Destroy(other.gameObject, 0f);
                break;
            default:
                break;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw player position
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(transform.position, 0.125f);
        
        // Draw forward movement ray
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, -transform.up*tree.TreeStats.GrowthSpeed.GetValue());
        
        // Draw the grid position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3Int.FloorToInt(transform.position) + Vector3.one/2f, Vector3.one);
    }

    // Updates the energy sliders
    private void UpdateEnergySliders()
    {
        // Update the main energy
        float mainEnergyFill = _totalEnergy / tree.TreeStats.Energy.GetValue();
        energySlider.value = mainEnergyFill;
        
        // Update the extra energy
        float extraEnergyFill = Mathf.Clamp(_totalEnergy + _extraEnergy, 0, tree.TreeStats.Energy.GetValue()) / tree.TreeStats.Energy.GetValue();
        extraEnergySlider.value = extraEnergyFill;
    }

    // Returns true if the root is currently grounded
    public bool IsRootGrounded()
    {
        // Get the tile position of the root
        Vector3Int rootGridPosition = dirtTilemap.WorldToCell(transform.position);

        // If there are no tiles under the root, the root is not grounded
        return dirtTilemap.GetTile(rootGridPosition);
    }

    // Updates the collider
    private void UpdateCollider()
    {
        // Set the width of the collider of the player
        float colliderWidth = Mathf.Max(_activeRootWidth, 0.1f);
        collider2D.size = new Vector2(colliderWidth, colliderWidth * 2f);
        collider2D.offset = new Vector2(0f, colliderWidth / 2f);
    }
    
    // Spawns a root and links it if needed
    private void GenerateRoot(Vector3 position, float startWidth)
    {
        // Create the root
        Root root = new Root(position, startWidth);
        
        // Spawn the line renderer
        LineRenderer lineRenderer = Instantiate(rootPrefab, position, Quaternion.identity, rootParent).GetComponent<LineRenderer>();
        
        // Line the line renderer to the root
        root.LineRenderer = lineRenderer;
        
        // Check if the parent has been assigned yet
        if (!_parentSpawned)
        {
            // This root is the parent
            _parentRoot = root;
            _parentSpawned = true;
        }
        else
        {
            _activeRoot.AddChild(root);
        }
    }
    
    // Sets the active root and resets the energy
    private void SetActiveRoot(Root root)
    {
        // Store the active root
        _activeRoot = root;
        
        // Reset the root properties
        _activeRootRenderer = root.LineRenderer;
        _activeRootPointIndex = 0;
        _activeRootDistanceTraveled = 0f;
        _activeRootWidth = root.Points[0].Width;
        
        // Reset the energy properties
        _totalEnergy = tree.TreeStats.Energy.GetValue();
        _extraEnergy = 0f;
        
        // Move the player position
        rigidbody2D.MovePosition(root.Points[0].Position);
    }
    
    // Checks if the game has been lost
    private void CheckGameOver()
    {
        if (tree.RootsRemaining <= 0)
        {
            // Game Over
            Debug.Log("Game Over");
        }
    }
    
    // Run at the end of a day to get ready for the next day
    private void FinishDay()
    {
        // Remove a single root from the remaining count
        tree.RootsRemaining--;
        
        // Update the root count UI
        rootCounter.text = $"Roots Remaining: {tree.RootsRemaining}";
        
        CheckGameOver();
        
        // Recap the day by reversing over active root to the start
        CollectRootHistory();
        cameraController.SetRecapTimer(recapLine);
        uiController.DisplayRecap(GetDayStats());
        
        // Store the root score to the tree score
        // Update the root score
        tree.RootScore = GetActiveRootScore();
        
        // Store the root score
        tree.StoreRoot();
    }
    
    // Starts the day
    public void StartDay()
    {
        // Add to the day counter
        Days++;
        
        transform.position = Vector3.zero;
        
        // Spawn the next root
        _parentSpawned = false;
        cameraController.Recapping = false;
        GenerateRoot(Vector3.zero, 1f);
        SetActiveRoot(_parentRoot);
    }
    
    // Collects the history points for the current recap
    private void CollectRootHistory()
    {
        // First get the points of the active root
        List<Vector3> rootPoints = new List<Vector3>();
        foreach (RootPoint rootPoint in _activeRoot.Points)
        {
            rootPoints.Add(rootPoint.Position);
        }
        
        // Simplfy the line
        List<Vector3> simplifiedRoot = new List<Vector3>();
        LineUtility.Simplify(rootPoints, 0.375f, simplifiedRoot);
        simplifiedRoot.Reverse();

        if (simplifiedRoot[simplifiedRoot.Count - 1] != Vector3.zero)
        {
            simplifiedRoot.Add(Vector3.zero);
        }

        // Store the new recap
        recapLine = simplifiedRoot;
    }

    // Returns the stats of the current day
    public DayStats GetDayStats()
    {
        float totalLength = _parentRoot.GetTotalLength();
        int rootsGrown = Root.GetChildCount(_parentRoot) + 1;

        return new DayStats(totalLength, rootsGrown);
    }
    
    // Returns a total current active root score
    private int GetActiveRootScore()
    {
        // Depth-based scoring
        // Each 15 tiles of depth are worth 100 pts
        int depthScore = Mathf.RoundToInt(Mathf.Floor(Mathf.Abs(Mathf.Clamp(Root.GetLowestDepth(_parentRoot), int.MinValue, 0)) / 15f) * 100f);
        // Modify the score based on stat modifiers
        depthScore = Mathf.RoundToInt(TreeStat.GetOneOffValue(tree.TreeStats.Scoring, depthScore));
        
        // Length-based scoring
        // Each 50 units of length are worth 100pts
        int lengthScore =  Mathf.RoundToInt(Mathf.Floor(_parentRoot.GetTotalLength()/50f)*100f);
        // Modify the score based on stat modifiers
        lengthScore = Mathf.RoundToInt(TreeStat.GetOneOffValue(tree.TreeStats.Scoring, lengthScore));

        Debug.Log($"DepthScore {depthScore} LengthScore {lengthScore} TotalScore {depthScore+lengthScore}");
        
        return depthScore + lengthScore;
    }
    

    [Serializable]
    public struct DayStats
    {
        public float DistanceGrown;
        public int RootsMade;

        public DayStats(float DistanceGrown, int RootsMade)
        {
            this.DistanceGrown = DistanceGrown;
            this.RootsMade = RootsMade;
        }
    }
}

// TODO: Make resistance thru different types of ground
// Add scoring - More options
// Make Root cards?
// Make tree have evolve bonus choices
// Make root power system where cards activated can increase root energy, speed, etc
// Regenerate split power ups each week?
// Make more powerups?

