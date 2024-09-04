using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using System.Linq;

using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;



public class NewBehaviourScript : MonoBehaviour
{
    public Unity.XR.CoreUtils.XROrigin sessionOrigin;

    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    public ARAnchorManager anchorManager;
    // public ARTrackedImageManager tiManager;

    public int flag = 2;
    public List<Vector3> coors;
    public Vector3 coor_avg;

    public IDataService DataService = new JsonDataService();
    public List<int> Jsondata;

    public GameObject[] v1, v2;
    public GameObject[] vehicles;

    public GameObject[] s1, s2;
    public GameObject[] slots;

    private GameObject parkinglot;
    public GameObject plane;
    public GameObject cube;

    GameObject[] FindObsWithTag(string tag)
    {
        GameObject[] foundObs = GameObject.FindGameObjectsWithTag(tag);
        Array.Sort(foundObs, CompareObNames);
        return foundObs;
    }

    int CompareObNames(GameObject x, GameObject y)
    {
        return x.name.CompareTo(y.name);
    }

    void RenderCar(List<int> slots)
    {
        int i = 0;
        foreach (GameObject vehicle in vehicles)
        {
            if (slots[i] == 1)
            {
                vehicle.GetComponent<Renderer>().enabled = true;
            }
            else
            {
                vehicle.GetComponent<Renderer>().enabled = false;
            }
            i++;
        }

    }

    void Start()
    {
        Jsondata = DataService.LoadData<List<int>>("/states.json", false);
        int jc = Jsondata.Count();

        v1 = FindObsWithTag("1stf_vehicle");
        v2 = FindObsWithTag("2ndf_vehicle");
        vehicles = v1.Concat(v2).ToArray();
        int vc = vehicles.Count();

        s1 = FindObsWithTag("1stf_slot");
        s2 = FindObsWithTag("2ndf_slot");
        slots = s1.Concat(s2).ToArray();

        if (jc != vc)
        {
            Debug.Log(jc);
            Debug.Log(vc);

            for (int i = 0; i < vc - jc; i++)
            {
                Jsondata.Add(0);
            }
        }
        Debug.Log(Jsondata.Count);

        parkinglot = GameObject.Find("Parkinglot");
        parkinglot.transform.position = new Vector3(0, 0, 0);
        parkinglot.SetActive(false);

        planeManager.enabled = true;
        plane.gameObject.SetActive(true);
    }

    void Update()
    {
        if (flag > 0)
        {
            if(Input.touchCount > 0)
            {
                if (Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    bool collision = raycastManager.Raycast(Input.mousePosition, raycastHits, TrackableType.PlaneWithinPolygon);
                    if (collision)
                    {
                        // Instantiate(cube, raycastHits[0].pose.position, Quaternion.identity);
                        coors.Add(raycastHits[0].pose.position);
                        flag -= 1;
                    }
                }
            }
        }
        else if(flag == 0)
        {
            Debug.Log(flag);
            foreach (var pos in coors)
            {
                coor_avg += pos;
            }
            // coor_avg = coor_avg / coors.Count;
            coor_avg.x /= 2;
            coor_avg.y = -1;
            coor_avg.z /= 2;

            parkinglot.transform.position = coor_avg;
            parkinglot.AddComponent<ARAnchor>();
            parkinglot.SetActive(true);

            foreach (var planes in planeManager.trackables)
            {
                planes.gameObject.SetActive(false);
            }
            planeManager.enabled = false;
            RenderCar(Jsondata);

            flag -= 1;
        }
        else
        {
            if (Input.touchCount > 0)
            {
                if (Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        var selection = hit.transform;
                        Debug.Log(selection);
                        if (selection != null)
                        {
                            int id = Array.IndexOf(slots, selection.GetComponent<Renderer>().gameObject);
                            if (id >= 0)
                            {
                                Debug.Log(id);
                                Jsondata[id] = 1 - Jsondata[id];
                                RenderCar(Jsondata);
                            }
                        }
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        DataService.SaveData("/states.json", Jsondata, false);
    }
}
