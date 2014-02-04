﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Planet : MonoBehaviour
{

    public int oil;
    public int coal;
    public int pollution;


    public GameState state;


    private Material originalMat;
    //private Material gridMat;

    public bool placeMode = false;
    private GameObject placeObject;

    public List<Building> placedBuildings;

    public Vector3 rotateVector = new Vector3(.1f, 1f, 0f);

    public float buildingRotation = 0;

    const int NUM_WIND_ZONES = 3;
    public GameObject[] windZones;


    // Use this for initialization
    void Start()
    {
        state = GameState.Instance;



        placedBuildings = new List<Building>();
        InvokeRepeating("UpdateState", 1f, 1.0f);


        Generate3DPerlinMap();
        GenerateWindZones();

    }

    void GenerateWindZones()
    {
        float radius = this.transform.localScale.x / 2.0f + 1;
        windZones = new GameObject[NUM_WIND_ZONES];

        for (int i = 0; i < NUM_WIND_ZONES; i++)
        {
            float theta = Random.Range(0f, Mathf.PI * 2f);
            float phi = Random.Range(0f, Mathf.PI);

            float x3d = radius * Mathf.Cos(theta) * Mathf.Sin(phi);
            float y3d = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
            float z3d = radius * -Mathf.Cos(phi);

            GameObject tornado = (GameObject)Instantiate(Resources.Load("prefabs/TornadoParticle"), new Vector3(x3d,y3d,z3d), Quaternion.identity);

            Ray planetRay = new Ray(tornado.transform.position, Vector3.zero - tornado.transform.position);

            RaycastHit[] hits = Physics.RaycastAll(planetRay);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.gameObject == state.planet.transform.gameObject)
                {
                    Quaternion q = Quaternion.LookRotation(hit.normal);
                    tornado.transform.localRotation = q;
                }
            }

            windZones[i] = tornado;

        }

        
    }

    void Generate3DPerlinMap()
    {
        

        Texture2D planetTex = new Texture2D(2048, 1024, TextureFormat.ARGB32, false);
        Texture2D cloudsTex = new Texture2D(2048, 1024, TextureFormat.ARGB32, false);
        
        Color[] cloudColors = cloudsTex.GetPixels();
        // set the pixel values
        Color[] colors = planetTex.GetPixels();

        Noise noise = new Noise();

        
        float pi = 3.14159265359f;
        float twopi = pi * 2.0f;

        float offsetx = (float)Random.Range(-200f, 200f);
        float offsety = (float)Random.Range(-200f, 200f);
        

        float min = 999;
        float max = -999;

        for (int y = 0; y < planetTex.height; y++)
        {
            int row = y * planetTex.width;
            for (int x = 0; x < planetTex.width; x++)
            {

                float theta = twopi * (x/(float)planetTex.width);
                float phi = pi * (y/(float)planetTex.height);

                float x3d = Mathf.Cos(theta) * Mathf.Sin(phi);
                float y3d = Mathf.Sin(theta) * Mathf.Sin(phi);
                float z3d = -Mathf.Cos(phi);



                float color = noise.fbm3(x3d*2+offsetx, y3d*2+offsety, z3d*2,3,2,2);
               
                int index = (int)(color * (state.planetColorRamp.colors.Length - 1));

                colors[row + x] = state.planetColorRamp.colors[index];
                cloudColors[row + x] = new Color(1, 1, 1, color);
                                               

            }
        }

        print("min:" + min + " max:" + max);
        cloudsTex.SetPixels(cloudColors);
        cloudsTex.Apply();
        GameObject.Find("Clouds").renderer.material.mainTexture = cloudsTex;
        planetTex.SetPixels(colors);
        planetTex.Apply();
        renderer.material.mainTexture = planetTex;
    }

    void Generate2DPerlinMap()
    {
        //originalMat = renderer.material;

        // Create a new 2x2 texture ARGB32 (32 bit with alpha) and no mipmaps
        //Texture2D originalTex = (Texture2D)originalMat.mainTexture;
        Texture2D planetTex = new Texture2D(2048, 2048, TextureFormat.ARGB32, false);
        Texture2D cloudsTex = new Texture2D(2048, 2048, TextureFormat.ARGB32, false);

        Color[] cloudColors = cloudsTex.GetPixels();
        // set the pixel values
        Color[] colors = planetTex.GetPixels();

        Noise noise = new Noise();




        float offsetx = (float)Random.Range(-200, 200);
        float offsety = (float)Random.Range(-200, 200);
        int row;
        for (int y = 0; y < planetTex.height; y++)
        {
            row = planetTex.width * y;
            for (int x = 0; x < planetTex.width; x++)
            {

                float color = noise.pfbm2(x / 256f + offsetx, y / 256f + offsety, planetTex.width / 256, planetTex.height / 256, 3, 2, 2);

                int index = (int)(color * (state.planetColorRamp.colors.Length - 1));
                colors[row + x] = state.planetColorRamp.colors[index];

                cloudColors[row + x] = new Color(1, 1, 1, color);


                //colors [planetTex.width * y + x] = new Color (color, color, color);	  
            }
        }

        cloudsTex.SetPixels(cloudColors);
        cloudsTex.Apply();
        GameObject.Find("Clouds").renderer.material.mainTexture = cloudsTex;
        // Apply all SetPixel calls
        planetTex.SetPixels(colors);
        planetTex.Apply();
        renderer.material.mainTexture = planetTex;
    }



    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButtonDown(1))
        {
            print("OIL MOUSE DOWN");
            if (state.oilScan && state.HasEnoughEnergyFor(state.scanCost))
            {
                print("SCANNING TRUE");
                state.UpdateOilMap();
                state.scanning = true;
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            state.scanning = false;
        }

    }

    void FixedUpdate()
    {
        //transform.RotateAround (Vector3.zero, rotateVector, Time.deltaTime * rotateSpeed);
        if (placeMode)
        {
            CheckMousePosition();
        }

    }

    private void UpdateState()
    {

        state.UpdateState();
    }

    void OnMouseDown()
    {

    }

    void OnMouseUp()
    {


    }

    void CheckMousePosition()
    {

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits;
        hits = Physics.RaycastAll(ray);
        if (hits.Length > 0)
        {

            bool hitPlanet = false;
            RaycastHit planetHit = new RaycastHit();

            for (int i = 0; i < hits.Length; i++)
            {

                if (hits[i].transform.gameObject == transform.gameObject)
                {
                    hitPlanet = true;
                    planetHit = hits[i];
                    break;
                }
            }


            if (hitPlanet && !state.scanning)
            {
                if (Input.GetMouseButton(1))
                {
                    buildingRotation = (buildingRotation + 100 * Time.deltaTime) % 360;
                }
                Texture2D surfaceTexture = (Texture2D)planetHit.transform.renderer.material.mainTexture;
                Color c = surfaceTexture.GetPixel((int)(planetHit.textureCoord.x * surfaceTexture.width), (int)(planetHit.textureCoord.y * surfaceTexture.height));
                Building b = (Building)placeObject.GetComponent(typeof(Building));
                b.CheckColor(c);

                Vector3 normal = planetHit.normal;
                Quaternion q = Quaternion.LookRotation(normal);
                placeObject.transform.localRotation = q;
                placeObject.transform.Rotate(90, 0, 0);
                //placeObject.transform.eulerAngles = new Vector3(placeObject.transform.localEulerAngles.x,placeObject.transform.localEulerAngles.y,buildingRotation);

                float adjustUpwards = (placeObject.transform.lossyScale.z / 2.0f) * .9f;
                placeObject.transform.position = planetHit.point;
                placeObject.transform.Translate(Vector3.up * adjustUpwards);

                //placeObject.rigidbody.MovePosition(planetHit.point);                
            }
            else
            {
                placeObject.transform.position = Vector3.zero;
            }
        }
        else
        {
            placeObject.transform.position = Vector3.zero;
        }
    }

    public void PlaceBuilding(Building b, Vector3 position)
    {
        placeMode = false;
        placeObject.transform.position = position;
        placeObject.transform.parent = transform;
        placeObject.rigidbody.mass = 999999;
        placedBuildings.Add(b);
        placeObject = null;


        //PlanetGUI.Instance.state = (int)PlanetGUI.GUI_STATE.BUILD_OPTIONS;
    }

    public void RemoveBuilding(Building b)
    {
        placedBuildings.Remove(b);
        Destroy(b.gameObject);
    }

    public void PlaceMode(string buildingType)
    {
        //Material[] mats = new Material[2];
        //mats[0] = originalMat; mats[1] = gridMat;
        //renderer.materials = mats;				
        if (placeMode)
            Destroy(placeObject);
        placeMode = true;
        placeObject = (GameObject)Instantiate(Resources.Load("prefabs/buildings/" + buildingType), Vector3.zero, Quaternion.identity);
        if (placeObject == null) print(buildingType);
    }



    public void CancelPlace()
    {
        Destroy(placeObject);
        placeObject = null;
        placeMode = false;
    }



}
