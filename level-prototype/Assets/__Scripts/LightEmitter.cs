using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LightEmitter : MonoBehaviour
{
    public bool debug;
    public GameObject pedestalPrefab;
    public GameObject prismPrefab;
    

    public int _maxReflectionCount = 5;
    public float _maxStepDistance = 200;
    public bool _drawPrediction;
    public bool _isActive = false;

    public List<GameObject> _activeCrystals;
    public GameObject _activePrism;
    public GameObject _parentLightEmitter;

    private LineRenderer _lineRenderer;
    private List<Vector3> _lineVertices;
    private Ray _ray;
    private RaycastHit _hit;

    private PlayerBehavior player;

    private float _floorHeight; //For intializing pedestal positions

    // Start is called before the first frame update
    void Start()
    {
        player = PlayerBehavior.S.GetComponent<PlayerBehavior>();
        _floorHeight = transform.parent.transform.position.y;
        _lineRenderer = GetComponent<LineRenderer>();
        _lineVertices = new List<Vector3>(_maxReflectionCount + 1);
        _activeCrystals = new List<GameObject>
        {
            this.gameObject
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (_isActive)
        {
            DrawLight();
            GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.yellow);
        }
        if (_parentLightEmitter != null && _parentLightEmitter.GetComponent<LightEmitter>()._activePrism != this.gameObject) 
        {
            DeactivatePrism();
        }
    }

    private void OnDrawGizmos()
    {

        if (_drawPrediction)
        {
            DrawPredictedReflectionPattern(this.transform.position + this.transform.forward * 0.75f, this.transform.forward, _maxReflectionCount);

        }
    }

    void DrawLight()
    {
        _activeCrystals.Clear();
        _lineVertices.Clear();
        _lineVertices.Add(this.transform.position);
        _ray = new Ray(_lineVertices[0], this.transform.forward);
        if (Physics.Raycast(_ray, out _hit, _maxStepDistance))
        {
            ReflectLineRenderer(_lineVertices[0], this.transform.forward, _maxReflectionCount);
        }
        else
        {
            _lineVertices.Add(this.transform.position + (this.transform.forward * _maxStepDistance));
        }
        if (!_activeCrystals.Contains(_activePrism)) _activePrism = null;
        _lineRenderer.positionCount = _lineVertices.Count;
        _lineRenderer.SetPositions(_lineVertices.ToArray());
    }

    void ReflectLineRenderer(Vector3 position, Vector3 direction, int reflectionsLeft) //LineRenderer
    {
        if (reflectionsLeft == 0) return;

       
        Ray ray = new Ray(position, direction);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, _maxStepDistance))
        {
            GameObject go = hit.collider.gameObject;
            switch (go.tag)
            {
                case "Mirror":
                    direction = (hit.collider.transform.position - _lineVertices[_lineVertices.Count-1]).normalized; //Direction fixed to mirror center
                    direction = Vector3.Reflect(direction, hit.normal);
                    position = hit.collider.transform.position; //Locks to mirror center
                    //position = hit.point; //Direct Hit anywhere on mirror
                    _lineVertices.Add(position);
                    ReflectLineRenderer(position, direction, reflectionsLeft - 1); //Reflect line again
                    break;
                case "Switch":
                    ActivateCrystal(hit.collider.gameObject);
                    position = hit.point;
                    _lineVertices.Add(position);
                    ReflectLineRenderer(hit.point + direction, direction, reflectionsLeft - 1); // the + direction makes it pass through collider
                    break;
                case "Player": //Sword Reflection
                    if (player.holdingSword && player.CanReflect(hit.point))
                    {
                        Vector3 origin = Camera.main.transform.position;
                        ray.origin = origin; //Ray origin is the camera
                        ray.direction = Camera.main.transform.forward;
                        origin -= player.transform.up; //Offset ray origin
                        origin += player.transform.forward;
                        position = origin; //So the line render starts slightly below
                        _lineVertices.Add(position);

                        if (Physics.Raycast(ray, out hit, _maxStepDistance))
                        {
                            direction = (hit.point - origin).normalized;
                        }
                        else
                            direction = Camera.main.transform.forward;
                        
                        ReflectLineRenderer(position, direction, reflectionsLeft - 1);
                    }
                    else
                    {
                        ReflectLineRenderer(hit.point + direction, direction, reflectionsLeft - 1);
                    }
                    break;
                case "Pedestal":
                case "Hole":
                    ReflectLineRenderer(hit.point + direction, direction, reflectionsLeft - 1);
                    break;
                case "Ghost":
                    KillGhost(hit.collider.gameObject);
                    ReflectLineRenderer(hit.point + direction, direction, reflectionsLeft - 1);
                    break;
                case "LightRay":
                    /*position = hit.point;
                    _lineVertices.Add(position);*/
                    if (go != this.gameObject)
                    {
                        ActivatePrism(go);
                    }
                    ReflectLineRenderer(hit.point + direction, direction, reflectionsLeft - 1);
                    return;
                default:
                    position = hit.point;
                    _lineVertices.Add(position);
                    return;
            }

        }
        else
        {
            position += direction * _maxStepDistance;
            _lineVertices.Add(position);
            return;
        }
        
        
    }

    void DrawPredictedReflectionPattern(Vector3 position, Vector3 direction, int reflectionsRemaining)
    {
        //Return if no more reflections
        if (reflectionsRemaining == 0) return;

        Vector3 startPos = position;

        //Raycast to detect collider, reflect if mirror
        Ray ray = new Ray(position, direction);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, _maxStepDistance))
        {
            string tag = hit.collider.gameObject.tag;
            if (tag == "Mirror")
            {
                direction = (hit.collider.transform.position - position).normalized;
                direction = Vector3.Reflect(direction, hit.normal);
                position = hit.collider.transform.position;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPos, position);
                DrawPredictedReflectionPattern(position, direction, reflectionsRemaining - 1);
            }
            else
            {
                position = hit.point;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPos, position);
                DrawPredictedReflectionPattern(position, direction, reflectionsRemaining - 1);
            }
        }
        else
        {
            position += direction * _maxStepDistance;
        }
    } //Gizmos prediction

    void ActivateCrystal(GameObject go)
    {
        CrystalSwitch crystal;
        crystal = go.GetComponent<CrystalSwitch>();
        crystal.Activate();
        crystal._lightEmitter = this.gameObject;
        if (!_activeCrystals.Contains(go))
        {
            _activeCrystals.Add(go);
        }

    }

    void ActivatePrism(GameObject go)
    {
        LightEmitter prism;
        prism = go.GetComponent<LightEmitter>();
        prism._isActive = true;
        _activePrism = go;
        prism._parentLightEmitter = this.gameObject;
        if (!_activeCrystals.Contains(go))
        {
            _activeCrystals.Add(go);
        }
        
    }

    public void DeactivatePrism()
    {
        _isActive = false;
        _parentLightEmitter = null;
        _activeCrystals.Clear();
        _lineVertices.Clear();
        _lineRenderer.positionCount = _lineVertices.Count;
        _lineRenderer.SetPositions(_lineVertices.ToArray());
    }

    private void KillGhost(GameObject go)
    {
        GhostBehavior ghost = go.GetComponent<GhostBehavior>();
        if(ghost.childGhost)
        {
            //Instantiate Prism
            GameObject prismGO = Instantiate<GameObject>(prismPrefab, go.transform.position, Quaternion.identity);
            Destroy(go);
        }
        else
        {
            GameObject pedestalGO = Instantiate<GameObject>(pedestalPrefab, go.transform.position, Quaternion.identity);
            PedestalScript pedestal = pedestalGO.transform.GetChild(2).GetComponent<PedestalScript>();
            pedestal.originalParent = go.transform.parent.parent;
            pedestal.transform.parent.parent = pedestal.originalParent;
            pedestal.hasMirror = false;
            pedestal.locked = false;
            Destroy(go);
        }
        
    }
}
