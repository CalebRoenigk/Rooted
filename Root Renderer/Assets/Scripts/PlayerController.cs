using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    
    // [SerializeField] private LineRenderer lineRenderer;

    // private float _distanceTraveled = 0f;
    // private int _lineRendererPointIndex = 0;
    
    // private Root _root;
    // private float _rootWidth = 1f;
    public bool IsGrounded = false;

    [SerializeField] private Tilemap dirtTilemap;
    
    [Header("Player")]
    [SerializeField] private float _playerSpeed = 1f;
    [SerializeField] private float _rotationalSpeed = 2f;
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

    private float _maxEnergy = 100f;
    
    [Header("UI")]
    [SerializeField] private Slider energySlider;
    [SerializeField] private Slider extraEnergySlider;
    

    // Start is called before the first frame update
    void Start()
    {
        GenerateRoot(Vector3.zero, 1f);
        SetActiveRoot(_parentRoot);
    }

    // Update is called once per frame
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
            rotationAngles.z += rotation * (_rotationalSpeed * Time.fixedDeltaTime);
            quaternion.eulerAngles = rotationAngles;
            rigidbody2D.MoveRotation(quaternion);
            // transform.eulerAngles = rotationAngles;
        
            // Get the down vector that will act as the forward vector of movement for the player
            Vector3 movementVector = -transform.up * (_playerSpeed * Time.fixedDeltaTime);
            
            // Add the gravity if any
            Vector2 gravityVector = Physics2D.gravity * (rigidbody2D.gravityScale * Time.fixedDeltaTime);
            movementVector += (Vector3)gravityVector;
        
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
            _activeRootWidth = _totalEnergy / _maxEnergy;
            
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
            
            // Check if the root is finished growing
            if (_extraEnergy <= 0f && _totalEnergy <= 0f)
            {
                // Check for ungrown child roots
                Root ungrownRoot = _activeRoot.GetUngrownRoot();

                if (!ungrownRoot.HasParent && ungrownRoot != _parentRoot)
                {
                    // There are no ungrown roots
                    Debug.Log("Root Growth finished!");
                }
                else
                {
                    // Start growing the ungrown root
                    SetActiveRoot(ungrownRoot);
                }
            }
        }
        
        // Update the energy slider visuals
        UpdateEnergySliders();
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
    }

    private void OnDrawGizmos()
    {
        // Draw player position
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(transform.position, 0.125f);
        
        // Draw forward movement ray
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, -transform.up*_playerSpeed);
    }

    // Updates the energy sliders
    private void UpdateEnergySliders()
    {
        // Update the main energy
        float mainEnergyFill = _totalEnergy / _maxEnergy;
        energySlider.value = mainEnergyFill;
        
        // Update the extra energy
        float extraEnergyFill = Mathf.Clamp(_totalEnergy + _extraEnergy, 0, _maxEnergy) / _maxEnergy;
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
        _totalEnergy = _maxEnergy;
        _extraEnergy = 0f;
        
        // Move the player position
        rigidbody2D.MovePosition(root.Points[0].Position);
    }
}
